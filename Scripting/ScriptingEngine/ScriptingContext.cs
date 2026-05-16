using Python.Runtime;
using Qenex.QSuite.LogSystems.LogSystem;
using Qenex.QSuite.Scripting.Script;
using Qenex.QSuite.Variables.QVariables;

namespace Qenex.QSuite.Scripting.ScriptingEngine;

public class ScriptingContext
{
    private readonly ILogger? logger;
    private readonly TaskFactory pythonFactory;
    private PythonLogWriter stdoutWriter = null!;
    private PythonLogWriter stderrWriter = null!;
    private CancellationTokenSource? periodicScriptsCts;
    private readonly List<Task> periodicScriptTasks = [];
    private static readonly object pythonInitLock = new();
    private static bool pythonRuntimeInitialized;
    
    #region Constructors

    public ScriptingContext(ScriptEngineSettings settings, ILogger? log = null)
    {
        logger = log;
        pythonFactory = new TaskFactory(new ConcurrentExclusiveSchedulerPair().ExclusiveScheduler);
        VariableBindings = [];
        EngineSettings = settings ?? throw new ArgumentNullException(nameof(settings));
        Scripts = new List<IScriptBase>();
    }

    #endregion

    #region Properties
    
    public IList<IScriptBase> Scripts { get; set; }
    public IList<VariableBinding> VariableBindings { get; set; }
    public ScriptEngineSettings EngineSettings { get; set; }
    public PyModule? SharedScope { get; internal set; }
    public event EventHandler<ScriptExecutedEventArgs>? ScriptExecuted;
    
    #endregion

    #region Scope
    

    public Task InitializeSharedScopeAsync(IList<IVariableBase> variables) =>
        pythonFactory.StartNew(() => InitializeSharedScope(variables));
    
    private void InitializeSharedScope(IList<IVariableBase> variables)
    {
        EnsurePythonRuntimeInitialized();
        
        var variablesById = variables.ToDictionary(v => v.Id);
        foreach (var varById in variablesById)
        {
            VariableBindings.Add(new VariableBinding(varById.Value.Name, varById.Key));
        }


        using (Py.GIL())
        {
            stdoutWriter = new PythonLogWriter(logger!, LogLevel.Script);
            stderrWriter = new PythonLogWriter(logger!, LogLevel.Script);
            
            using (var sys = Py.Import("sys"))
            {
                sys.SetAttr("stdout", stdoutWriter.ToPython());
                sys.SetAttr("stderr", stderrWriter.ToPython());
            }
            
            SharedScope = Py.CreateScope("qenex_scripts_shared");

            foreach (var binding in VariableBindings)
            {
                if (!variablesById.TryGetValue(binding.VariableId, out var variable))
                {
                    logger?.Log(LogLevel.Warn, $"Variable {binding.VariableId} not found.");
                    continue;
                }

                SharedScope.Set(binding.PythonName, new VariableBridge(variable));
            }

            //SharedScope.Exec("print(\"Ahoj - toto je test\")");
        }
        
        // Execute startup scripts - executed only once.
        ExecuteScripts(ScriptExecutionMode.Startup);
        StartPeriodicScripts();
    }

    private void EnsurePythonRuntimeInitialized()
    {
        if (pythonRuntimeInitialized) return;

        lock (pythonInitLock)
        {
            if (pythonRuntimeInitialized) return;
            if (PythonEngine.IsInitialized) { pythonRuntimeInitialized = true; return; }

            if (string.IsNullOrWhiteSpace(EngineSettings.PythonDllPath))
            {
                throw new InvalidOperationException("Python DLL path is not configured.");
            }

            if (!File.Exists(EngineSettings.PythonDllPath))
            {
                //logger?.Log(LogLevel.Error, $"Configured Python DLL path was not found: {EngineSettings.PythonDllPath}");
                throw new FileNotFoundException("Configured Python DLL path was not found.", EngineSettings.PythonDllPath);
            }

            Runtime.PythonDLL = EngineSettings.PythonDllPath;
            PythonEngine.Initialize();
            PythonEngine.BeginAllowThreads();
            pythonRuntimeInitialized = true;
        }
    }
    
    public async Task DisposeSharedScopeAsync()
    {
        await StopPeriodicScriptsAsync();
        await pythonFactory.StartNew(DisposeSharedScope);
    }

    private void DisposeSharedScope()
    {
        // Execute script at the end - log, etc.
        ExecuteScripts(ScriptExecutionMode.Shutdown);
        
        using (Py.GIL())
        {
            using (var sys = Py.Import("sys"))
            {
                sys.SetAttr("stdout", sys.GetAttr("__stdout__"));
                sys.SetAttr("stderr", sys.GetAttr("__stderr__"));
            }
            
            SharedScope?.Dispose();
            SharedScope = null;
        }
        stdoutWriter = null!;
        stderrWriter = null!;
    }

    #endregion

    #region Scripts
    
    public void AddScript(IScriptBase script)
    {
        if (Scripts.FirstOrDefault(s => s.FileName == script.FileName) != null)
        {
            logger?.Log(LogLevel.Warn, $"Script with Name {script.FileName} already exists.");
            return;
        }
        Scripts.Add(script);
    }

    public void AddScripts(IList<IScriptBase> scripts)
    {
        foreach (var script in scripts)
        {
            AddScript(script);
        }
    }

    public void RemoveScript(IScriptBase script)
    {
        Scripts.Remove(script);
    }

    private void ExecuteScripts(ScriptExecutionMode scriptMode)
    {
        var scripts = Scripts.Where(s => s.ExecutionMode == scriptMode);
        foreach (var script in scripts)
        {
            ExecuteScript(script);
        }
    }

    private void ExecuteScript(IScriptBase script)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        script.RunState = ScriptRunState.Running;

        try
        {
            using (Py.GIL())
            {
                SharedScope!.Exec(script.Content);
            }

            script.RunState = ScriptRunState.Idle;
        }
        catch (PythonException e)
        {
            script.RunState = ScriptRunState.Faulted;
            logger?.Log(LogLevel.Warn, $"python script \"{script.FileName}\": {e.Message}");
        }
        catch (Exception e)
        {
            script.RunState = ScriptRunState.Faulted;
            logger?.Log(LogLevel.Error, $"script \"{script.FileName}\": {e.Message}");
        }
        finally
        {
            stopwatch.Stop();
            script.LastExecutionDurationMs = stopwatch.Elapsed.TotalMilliseconds;
            OnScriptExecuted(script);
        }
    }

    private void StartPeriodicScripts()
    {
        if (periodicScriptsCts != null)
        {
            return;
        }

        periodicScriptsCts = new CancellationTokenSource();
        foreach (var script in Scripts.Where(s => s.ExecutionMode == ScriptExecutionMode.Periodic))
        {
            if (!TryGetPeriodicInterval(script, out var interval))
            {
                continue;
            }

            periodicScriptTasks.Add(RunPeriodicScriptAsync(script, interval, periodicScriptsCts.Token));
        }
    }

    private async Task StopPeriodicScriptsAsync()
    {
        if (periodicScriptsCts == null)
        {
            return;
        }

        await periodicScriptsCts.CancelAsync();

        try
        {
            await Task.WhenAll(periodicScriptTasks);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            periodicScriptsCts.Dispose();
            periodicScriptsCts = null;
            periodicScriptTasks.Clear();
        }
    }

    private async Task RunPeriodicScriptAsync(IScriptBase script, TimeSpan interval, CancellationToken ct)
    {
        using var timer = new PeriodicTimer(interval);

        try
        {
            while (await timer.WaitForNextTickAsync(ct))
            {
                await pythonFactory.StartNew(() => ExecuteScript(script), ct);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private bool TryGetPeriodicInterval(IScriptBase script, out TimeSpan interval)
    {
        interval = TimeSpan.Zero;
        var settings = ParseAdditionalInfo(script.AdditionalInfo);

        if (!settings.TryGetValue("period", out var periodText) ||
            !double.TryParse(periodText, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var period) ||
            period <= 0)
        {
            logger?.Log(LogLevel.Warn, $"Periodic script \"{script.FileName}\" has invalid period setting.");
            return false;
        }

        if (!settings.TryGetValue("unit", out var unit))
        {
            logger?.Log(LogLevel.Warn, $"Periodic script \"{script.FileName}\" has no unit setting.");
            return false;
        }

        interval = unit.Trim().ToLowerInvariant() switch
        {
            "ms" or "msec" or "millisec" or "millisecond" or "milliseconds" => TimeSpan.FromMilliseconds(period),
            "s" or "sec" or "second" or "seconds" => TimeSpan.FromSeconds(period),
            "m" or "min" or "minute" or "minutes" => TimeSpan.FromMinutes(period),
            "h" or "hour" or "hours" => TimeSpan.FromHours(period),
            _ => TimeSpan.Zero
        };

        if (interval > TimeSpan.Zero)
        {
            return true;
        }

        logger?.Log(LogLevel.Warn, $"Periodic script \"{script.FileName}\" has unsupported unit setting \"{unit}\".");
        return false;
    }

    private static Dictionary<string, string> ParseAdditionalInfo(string additionalInfo)
    {
        var settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var parts = additionalInfo.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var part in parts)
        {
            var keyValue = part.Split('=', 2, StringSplitOptions.TrimEntries);
            if (keyValue.Length == 2)
            {
                settings[keyValue[0]] = keyValue[1];
            }
        }

        return settings;
    }
    
    #endregion

    private void OnScriptExecuted(IScriptBase script)
    {
        ScriptExecuted?.Invoke(this, new ScriptExecutedEventArgs(script));
    }
}

public class ScriptExecutedEventArgs(IScriptBase script) : EventArgs
{
    public IScriptBase Script { get; } = script;
}

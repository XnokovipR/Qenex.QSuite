using Python.Runtime;
using System.Globalization;
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
    private readonly Dictionary<int, double> lastOnValueChangedValues = [];
    private static readonly object pythonInitLock = new();
    private static bool pythonRuntimeInitialized;
    
    #region Constructors

    public ScriptingContext(ScriptEngineSettings settings, ILogger? log = null)
    {
        logger = log;
        pythonFactory = new TaskFactory(new ConcurrentExclusiveSchedulerPair().ExclusiveScheduler);
        VariableBindings = [];
        OnValueChangedScriptTriggers = [];
        EngineSettings = settings ?? throw new ArgumentNullException(nameof(settings));
        Scripts = new List<IScriptBase>();
    }

    #endregion

    #region Properties
    
    public IList<IScriptBase> Scripts { get; set; }
    public IList<VariableBinding> VariableBindings { get; set; }
    public IList<OnValueChangedScriptTrigger> OnValueChangedScriptTriggers { get; set; }
    public ScriptEngineSettings EngineSettings { get; set; }
    public PyModule? SharedScope { get; internal set; }
    public event EventHandler<ScriptExecutedEventArgs>? ScriptExecuted;
    
    #endregion

    #region Scope
    

    public async Task InitializeSharedScopeAsync(IList<IVariableBase> variables)
    {
        await pythonFactory.StartNew(() => InitializeSharedScope(variables));
        await ExecuteScriptsAsync(ScriptExecutionMode.Startup, allowNonBlocking: true);
        StartPeriodicScripts();
    }
    
    private void InitializeSharedScope(IList<IVariableBase> variables)
    {
        EnsurePythonRuntimeInitialized();
        VariableBindings.Clear();
        lastOnValueChangedValues.Clear();
        
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
        await ExecuteScriptsAsync(ScriptExecutionMode.Shutdown, allowNonBlocking: false);
        await pythonFactory.StartNew(DisposeSharedScope);
    }

    private void DisposeSharedScope()
    {
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

    public void AddOnValueChangedScriptTrigger(int variableId, string scriptFileName, string additionalInfo)
    {
        if (string.IsNullOrWhiteSpace(scriptFileName))
        {
            logger?.Log(LogLevel.Warn, $"OnValueChanged script trigger for variable {variableId} has empty script reference.");
            return;
        }

        if (OnValueChangedScriptTriggers.Any(t =>
                t.VariableId == variableId &&
                t.ScriptFileName == scriptFileName &&
                t.AdditionalInfo == additionalInfo))
        {
            return;
        }

        OnValueChangedScriptTriggers.Add(new OnValueChangedScriptTrigger(variableId, scriptFileName, additionalInfo));
    }

    public IEnumerable<OnValueChangedScriptTrigger> GetOnValueChangedScriptTriggers(int variableId)
    {
        return OnValueChangedScriptTriggers.Where(t => t.VariableId == variableId);
    }

    public bool HasOnValueChangedScriptTriggers(int variableId)
    {
        return OnValueChangedScriptTriggers.Any(t => t.VariableId == variableId);
    }

    public Task HandleVariableValueChangedAsync(IVariableBase variable)
    {
        var value = variable.GetValue();
        return pythonFactory.StartNew(() => HandleVariableValueChanged(variable.Id, variable.Name, value));
    }

    private async Task ExecuteScriptsAsync(ScriptExecutionMode scriptMode, bool allowNonBlocking)
    {
        var scripts = Scripts.Where(s => s.ExecutionMode == scriptMode);
        foreach (var script in scripts)
        {
            var options = GetScriptExecutionOptions(script);
            if (allowNonBlocking && !options.Blocking)
            {
                _ = RunNonBlockingScriptAsync(script, options);
                continue;
            }

            await ExecuteScriptAsync(script, options);
        }
    }

    private async Task RunNonBlockingScriptAsync(IScriptBase script, ScriptExecutionOptions options)
    {
        try
        {
            await ExecuteScriptAsync(script, options);
        }
        catch (Exception e)
        {
            logger?.Log(LogLevel.Error, $"script \"{script.FileName}\" background execution failed: {e.Message}");
        }
    }

    private async Task ExecuteScriptAsync(IScriptBase script, ScriptExecutionOptions options, CancellationToken ct = default)
    {
        if (!script.IsEnabled)
        {
            return;
        }

        var executionTask = pythonFactory.StartNew(() => ExecuteScript(script), ct);
        if (options.Timeout == null)
        {
            await executionTask;
            return;
        }

        var timeoutTask = Task.Delay(options.Timeout.Value, ct);
        if (await Task.WhenAny(executionTask, timeoutTask) == executionTask)
        {
            await executionTask;
            return;
        }

        script.RunState = ScriptRunState.Faulted;
        logger?.Log(LogLevel.Warn, $"script \"{script.FileName}\" timed out after {options.Timeout.Value.TotalMilliseconds:0} ms.");
        OnScriptExecuted(script);
    }

    private void ExecuteScript(IScriptBase script, Action? beforeExecute = null)
    {
        if (!script.IsEnabled)
        {
            return;
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        script.RunState = ScriptRunState.Running;

        try
        {
            using (Py.GIL())
            {
                beforeExecute?.Invoke();
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

    private void HandleVariableValueChanged(int variableId, string variableName, object value)
    {
        if (!TryConvertToDouble(value, out var newValue))
        {
            logger?.Log(LogLevel.Warn, $"OnValueChanged script trigger for variable \"{variableName}\" skipped because value \"{value}\" is not numeric.");
            return;
        }

        if (!lastOnValueChangedValues.TryGetValue(variableId, out var oldValue))
        {
            lastOnValueChangedValues[variableId] = newValue;
            return;
        }

        // lastOnValueChangedValues[variableId] = newValue;
        var delta = Math.Abs(newValue - oldValue);

        foreach (var trigger in GetOnValueChangedScriptTriggers(variableId))
        {
            if (!TryGetOnValueChangedThreshold(trigger, out var threshold))
            {
                continue;
            }

            if (delta <= threshold)
            {
                continue;
            }

            ExecuteOnValueChangedScript(trigger, variableName, oldValue, newValue, delta);
            lastOnValueChangedValues[variableId] = newValue;
        }
    }

    private void ExecuteOnValueChangedScript(OnValueChangedScriptTrigger trigger, string variableName, double oldValue, double newValue, double delta)
    {
        var script = Scripts.FirstOrDefault(s => s.FileName == trigger.ScriptFileName);
        if (script == null)
        {
            logger?.Log(LogLevel.Warn, $"OnValueChanged script \"{trigger.ScriptFileName}\" was not found.");
            return;
        }

        if (script.ExecutionMode != ScriptExecutionMode.OnValueChanged)
        {
            logger?.Log(LogLevel.Warn, $"Script \"{script.FileName}\" is referenced by an OnValueChanged trigger but has execution mode {script.ExecutionMode}.");
            return;
        }

        ExecuteScript(script, () =>
        {
            SharedScope!.Set("qenex_trigger_variable_id", trigger.VariableId);
            SharedScope.Set("qenex_trigger_variable_name", variableName);
            SharedScope.Set("qenex_trigger_old_value", oldValue);
            SharedScope.Set("qenex_trigger_new_value", newValue);
            SharedScope.Set("qenex_trigger_delta", delta);
        });
    }

    private bool TryGetOnValueChangedThreshold(OnValueChangedScriptTrigger trigger, out double threshold)
    {
        threshold = 0;
        var settings = ParseAdditionalInfo(trigger.AdditionalInfo);

        if (!settings.TryGetValue("threshold", out var thresholdText) || string.IsNullOrWhiteSpace(thresholdText))
        {
            return true;
        }

        if (double.TryParse(thresholdText, NumberStyles.Float, CultureInfo.InvariantCulture, out threshold))
        {
            threshold = Math.Abs(threshold);
            return true;
        }

        logger?.Log(LogLevel.Warn, $"OnValueChanged script \"{trigger.ScriptFileName}\" has invalid threshold setting \"{thresholdText}\".");
        return false;
    }

    private static bool TryConvertToDouble(object value, out double result)
    {
        try
        {
            result = Convert.ToDouble(value, CultureInfo.InvariantCulture);
            return true;
        }
        catch
        {
            result = 0;
            return false;
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
                await ExecuteScriptAsync(script, GetScriptExecutionOptions(script), ct);
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

    private ScriptExecutionOptions GetScriptExecutionOptions(IScriptBase script)
    {
        var settings = ParseAdditionalInfo(script.AdditionalInfo);
        var blocking = true;
        TimeSpan? timeout = null;

        if (settings.TryGetValue("blocking", out var blockingText) && !TryParseBoolean(blockingText, out blocking))
        {
            logger?.Log(LogLevel.Warn, $"Script \"{script.FileName}\" has invalid blocking setting \"{blockingText}\".");
            blocking = true;
        }

        if (settings.TryGetValue("timeout", out var timeoutText))
        {
            if (double.TryParse(timeoutText, NumberStyles.Float, CultureInfo.InvariantCulture, out var timeoutMs) && timeoutMs > 0)
            {
                timeout = TimeSpan.FromMilliseconds(timeoutMs);
            }
            else
            {
                logger?.Log(LogLevel.Warn, $"Script \"{script.FileName}\" has invalid timeout setting \"{timeoutText}\".");
            }
        }

        return new ScriptExecutionOptions(blocking, timeout);
    }

    private static bool TryParseBoolean(string value, out bool result)
    {
        switch (value.Trim().ToLowerInvariant())
        {
            case "true":
            case "1":
            case "yes":
            case "y":
                result = true;
                return true;
            case "false":
            case "0":
            case "no":
            case "n":
                result = false;
                return true;
            default:
                result = false;
                return false;
        }
    }
    
    #endregion

    private void OnScriptExecuted(IScriptBase script)
    {
        ScriptExecuted?.Invoke(this, new ScriptExecutedEventArgs(script));
    }
}

internal readonly record struct ScriptExecutionOptions(bool Blocking, TimeSpan? Timeout);

public class ScriptExecutedEventArgs(IScriptBase script) : EventArgs
{
    public IScriptBase Script { get; } = script;
}

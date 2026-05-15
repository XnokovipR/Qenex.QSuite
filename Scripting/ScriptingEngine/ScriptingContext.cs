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
        
        // Execute startup scripts - executed only once
        ExecuteScripts(ScriptExecutionMode.Startup);
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
    
    public Task DisposeSharedScopeAsync() => pythonFactory.StartNew(DisposeSharedScope);

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
        var startupScripts = Scripts.Where(s => s.ExecutionMode == scriptMode);
        foreach (var script in startupScripts)
        {
            try
            {
                using (Py.GIL())
                {
                    SharedScope!.Exec(script.Content);
                }
            }
            catch (PythonException e)
            {
                logger?.Log(LogLevel.Warn, $"python script \"{script.FileName}\": {e.Message}");
            }
        }
    }
    
    #endregion
}

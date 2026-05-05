using Python.Runtime;
using Qenex.QSuite.LogSystems.LogSystem;
using Qenex.QSuite.Scripting.Script;
using Qenex.QSuite.Variables.QVariables;

namespace Qenex.QSuite.Scripting.ScriptingEngine;

public class ScriptingContext
{
    private readonly ILogger? logger;
    private readonly TaskFactory pythonFactory;
    private PythonLogWriter stdoutWriter;
    private PythonLogWriter stderrWriter;
    
    #region Constructors

    public ScriptingContext(ILogger? log = null)
    {
        logger = log;
        pythonFactory = new TaskFactory(new ConcurrentExclusiveSchedulerPair().ExclusiveScheduler);
        VariableBindings = [];
        EngineSettings = new ScriptEngineSettings();
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
        var pythonDll = @"c:\Users\radek\AppData\Local\Python\pythoncore-3.13-64\python313.dll ";
        Runtime.PythonDLL = pythonDll;
        PythonEngine.Initialize();
        PythonEngine.BeginAllowThreads();
        
        var variablesById = variables.ToDictionary(v => v.Id);
        foreach (var varById in variablesById)
        {
            VariableBindings.Add(new VariableBinding(varById.Value.Name, varById.Key));
        }


        using (Py.GIL())
        {
            stdoutWriter = new PythonLogWriter(logger!, LogLevel.Info);
            stderrWriter = new PythonLogWriter(logger!, LogLevel.Error);
            
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
        }
        
        // Execute startup scripts - executed only once
        ExecuteScripts(ScriptExecutionMode.Startup);
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
                SharedScope!.Exec(script.Content);
            }
            catch (PythonException e)
            {
                logger?.Log(LogLevel.Warn, "Python script exception", e);
            }
        }
    }
    
    #endregion
}
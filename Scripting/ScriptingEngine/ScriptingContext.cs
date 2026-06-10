using Python.Runtime;
using System.Globalization;
using System.Text;
using Qenex.QSuite.LogSystems.LogSystem;
using Qenex.QSuite.Scripting.Script;
using Qenex.QSuite.Variables.QVariables;

namespace Qenex.QSuite.Scripting.ScriptingEngine;

public class ScriptingContext
{
    private const string LastScriptExceptionTextName = "__qenex_script_exception_text";
    private const string ScriptStopTokenName = "__qenex_stop_token";
    private const double ScriptOverloadWarningRatio = 0.7;
    private static readonly TimeSpan StopWaitTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan ScriptOverloadWarningInterval = TimeSpan.FromSeconds(5);
    private readonly ILogger? logger;
    private readonly TaskFactory pythonFactory;
    private PythonLogWriter stdoutWriter = null!;
    private PythonLogWriter stderrWriter = null!;
    private CancellationTokenSource? periodicScriptsCts;
    private readonly List<Task> periodicScriptTasks = [];
    private readonly object executionStateLock = new();
    private readonly object onValueChangedStateLock = new();
    private readonly object scriptOverloadStateLock = new();
    private readonly HashSet<IScriptBase> scheduledScripts = [];
    private readonly HashSet<IScriptBase> interruptedScripts = [];
    private readonly HashSet<IScriptBase> notifiedInterruptedScripts = [];
    private readonly Dictionary<int, double> lastOnValueChangedValues = [];
    private readonly Dictionary<string, ScriptCallOverloadState> scriptOverloadStates = [];
    private CancellationTokenSource activeExecutionCts = new();
    private bool isStopping;
    private bool suppressLateExecutionOutput;
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
    public bool IsReplayMode { get; set; }
    public PyModule? SharedScope { get; internal set; }
    public bool HasAbandonedExecutions { get; private set; }
    public event EventHandler<ScriptExecutedEventArgs>? ScriptExecuted;
    
    #endregion

    #region Scope
    

    public async Task InitializeSharedScopeAsync(IList<IVariableBase> variables)
    {
        ResetStopState();
        await pythonFactory.StartNew(() => InitializeSharedScope(variables));
        await ExecuteScriptsAsync(ScriptExecutionMode.Startup, allowNonBlocking: true);
        StartPeriodicScripts();
    }
    
    private void InitializeSharedScope(IList<IVariableBase> variables)
    {
        if (HasAbandonedExecutions)
        {
            throw new InvalidOperationException("Abandoned scripting context cannot be restarted.");
        }

        EnsurePythonRuntimeInitialized();
        VariableBindings.Clear();
        lastOnValueChangedValues.Clear();
        ClearScriptOverloadState();
        
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

    public Task<InteractivePythonExecutionResult> ExecuteInteractiveAsync(string input, CancellationToken ct = default)
    {
        return pythonFactory.StartNew(() => ExecuteInteractive(input), ct);
    }

    private InteractivePythonExecutionResult ExecuteInteractive(string input)
    {
        if (SharedScope == null)
        {
            throw new InvalidOperationException("Python shared scope is not initialized.");
        }

        using (Py.GIL())
        {
            EnsureInteractiveConsole();

            var stdoutWriter = new PythonOutputBufferWriter();
            var stderrWriter = new PythonOutputBufferWriter();

            using var sys = Py.Import("sys");
            using var previousStdout = sys.GetAttr("stdout");
            using var previousStderr = sys.GetAttr("stderr");

            try
            {
                sys.SetAttr("stdout", stdoutWriter.ToPython());
                sys.SetAttr("stderr", stderrWriter.ToPython());

                using var console = SharedScope.Get("__qenex_interactive_console");
                using var result = console.InvokeMethod("push", input.ToPython());

                return new InteractivePythonExecutionResult
                {
                    Output = stdoutWriter.Text,
                    Error = stderrWriter.Text,
                    IsIncomplete = result.As<bool>()
                };
            }
            finally
            {
                sys.SetAttr("stdout", previousStdout);
                sys.SetAttr("stderr", previousStderr);
            }
        }
    }

    private void EnsureInteractiveConsole()
    {
        SharedScope!.Exec("""
import code as __qenex_code
if "__qenex_interactive_console" not in globals():
    __qenex_interactive_console = __qenex_code.InteractiveConsole(locals())
""");
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
    
    public async Task DisposeSharedScopeAsync(CancellationToken ct = default)
    {
        RequestStop();
        await StopPeriodicScriptsAsync();
        if (!HasAbandonedExecutions)
        {
            await ExecuteScriptsAsync(
                ScriptExecutionMode.Shutdown,
                allowNonBlocking: false,
                ct,
                allowStopCancellation: false,
                defaultTimeout: StopWaitTimeout);
        }
        else
        {
            logger?.Log(LogLevel.Warn, "Shutdown scripts skipped because scripting context has abandoned executions.");
        }

        await RunPythonFactoryActionAsync(DisposeSharedScope, "disposing Python shared scope", StopWaitTimeout, ct);
    }

    public void RequestStop()
    {
        isStopping = true;
        MarkScheduledExecutionsInterruptedOnStop();
        if (!activeExecutionCts.IsCancellationRequested)
        {
            activeExecutionCts.Cancel();
        }
    }

    public ScriptingContext CreateCleanContextForNextSession()
    {
        return new ScriptingContext(EngineSettings, logger)
        {
            Scripts = Scripts,
            OnValueChangedScriptTriggers = OnValueChangedScriptTriggers,
            IsReplayMode = IsReplayMode,
            ScriptExecuted = ScriptExecuted
        };
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

    public async Task HandleVariableValueChangedAsync(IVariableBase variable)
    {
        var value = variable.GetValue();
        await HandleVariableValueChangedAsync(variable.Id, variable.Name, value);
    }

    private async Task ExecuteScriptsAsync(
        ScriptExecutionMode scriptMode,
        bool allowNonBlocking,
        CancellationToken ct = default,
        bool allowStopCancellation = true,
        TimeSpan? defaultTimeout = null)
    {
        var scripts = Scripts.Where(s => s.ExecutionMode == scriptMode);
        foreach (var script in scripts)
        {
            var options = GetScriptExecutionOptions(script, defaultTimeout);
            if (allowNonBlocking && !options.Blocking)
            {
                _ = RunNonBlockingScriptAsync(script, options, allowStopCancellation);
                continue;
            }

            await ExecuteScriptAsync(script, options, ct, allowStopCancellation: allowStopCancellation);
        }
    }

    private async Task RunNonBlockingScriptAsync(
        IScriptBase script,
        ScriptExecutionOptions options,
        bool allowStopCancellation)
    {
        try
        {
            await ExecuteScriptAsync(script, options, allowStopCancellation: allowStopCancellation);
        }
        catch (Exception e)
        {
            logger?.Log(LogLevel.Error, $"script \"{script.FileName}\" background execution failed: {e.Message}");
        }
    }

    private async Task<bool> ExecuteScriptAsync(
        IScriptBase script,
        ScriptExecutionOptions options,
        CancellationToken ct = default,
        Action? beforeExecute = null,
        bool allowStopCancellation = true)
    {
        if (!CanExecuteScript(script))
        {
            return false;
        }

        using var linkedCts = allowStopCancellation
            ? CancellationTokenSource.CreateLinkedTokenSource(ct, activeExecutionCts.Token)
            : CancellationTokenSource.CreateLinkedTokenSource(ct);

        if (linkedCts.IsCancellationRequested)
        {
            return false;
        }

        if (!TryReserveScriptExecution(script))
        {
            return false;
        }

        var executionTask = pythonFactory.StartNew(() => ExecuteScript(script, beforeExecute, linkedCts.Token), linkedCts.Token);
        ReleaseScriptExecutionWhenCompleted(executionTask, script);
        var cancellationTask = Task.Delay(Timeout.InfiniteTimeSpan, linkedCts.Token);
        Task completedTask;

        if (options.Timeout == null)
        {
            completedTask = await Task.WhenAny(executionTask, cancellationTask);
        }
        else
        {
            var timeoutTask = Task.Delay(options.Timeout.Value, ct);
            completedTask = await Task.WhenAny(executionTask, cancellationTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                MarkScriptInterrupted(script, $"script \"{script.FileName}\" timed out after {options.Timeout.Value.TotalMilliseconds:0} ms.");
                await linkedCts.CancelAsync();
                ObserveBackgroundTask(executionTask, $"script \"{script.FileName}\" after timeout");
                return false;
            }
        }

        if (completedTask == executionTask)
        {
            await AwaitExecutionTaskAsync(executionTask, ct);
            return true;
        }

        MarkScriptInterrupted(script, $"script \"{script.FileName}\" was interrupted because scripting context is stopping.");
        await linkedCts.CancelAsync();
        ObserveBackgroundTask(executionTask, $"script \"{script.FileName}\" after stop request");
        return false;
    }

    private void ExecuteScript(IScriptBase script, Action? beforeExecute = null, CancellationToken ct = default)
    {
        if (!CanExecuteScript(script))
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
                SharedScope!.Set(ScriptStopTokenName, new ScriptStopToken(ct));
                var pythonError = ExecuteScriptContent(script.Content);
                if (!string.IsNullOrWhiteSpace(pythonError))
                {
                    if (!ShouldSuppressScriptResult(script))
                    {
                        script.RunState = ScriptRunState.Faulted;
                        logger?.Log(LogLevel.Warn, $"python script \"{script.FileName}\": {pythonError}");
                    }

                    return;
                }
            }

            if (!ShouldSuppressScriptResult(script))
            {
                script.RunState = ScriptRunState.Idle;
            }
        }
        catch (PythonException e)
        {
            if (!ShouldSuppressScriptResult(script))
            {
                script.RunState = ScriptRunState.Faulted;
                logger?.Log(LogLevel.Warn, $"python script \"{script.FileName}\": {e.Message}");
            }
        }
        catch (Exception e)
        {
            if (!ShouldSuppressScriptResult(script))
            {
                script.RunState = ScriptRunState.Faulted;
                logger?.Log(LogLevel.Error, $"script \"{script.FileName}\": {e.Message}");
            }
        }
        finally
        {
            if (!ShouldSuppressScriptNotification(script))
            {
                stopwatch.Stop();
                script.LastExecutionDurationMs = stopwatch.Elapsed.TotalMilliseconds;
                OnScriptExecuted(script);
            }
        }
    }

    private bool CanExecuteScript(IScriptBase script)
    {
        if (isStopping && script.ExecutionMode != ScriptExecutionMode.Shutdown)
        {
            return false;
        }

        return IsReplayMode
            ? script.IsReplayEnabled
            : script.IsEnabled;
    }

    private async Task HandleVariableValueChangedAsync(int variableId, string variableName, object value)
    {
        if (isStopping)
        {
            return;
        }

        if (!TryConvertToDouble(value, out var newValue))
        {
            logger?.Log(LogLevel.Warn, $"OnValueChanged script trigger for variable \"{variableName}\" skipped because value \"{value}\" is not numeric.");
            return;
        }

        List<(OnValueChangedScriptTrigger Trigger, double OldValue, double NewValue, double Delta)> scriptExecutions = [];
        lock (onValueChangedStateLock)
        {
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

                scriptExecutions.Add((trigger, oldValue, newValue, delta));
                lastOnValueChangedValues[variableId] = newValue;
            }
        }

        foreach (var scriptExecution in scriptExecutions)
        {
            await ExecuteOnValueChangedScriptAsync(
                scriptExecution.Trigger,
                variableName,
                scriptExecution.OldValue,
                scriptExecution.NewValue,
                scriptExecution.Delta);
        }
    }

    private async Task ExecuteOnValueChangedScriptAsync(
        OnValueChangedScriptTrigger trigger,
        string variableName,
        double oldValue,
        double newValue,
        double delta)
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

        var overloadKey = GetScriptOverloadKey("onvaluechanged", script);
        if (!TryEnterObservedScriptCall(overloadKey, out var observedCallPeriod))
        {
            return;
        }

        var executed = await ExecuteScriptAsync(script, GetScriptExecutionOptions(script), beforeExecute: () =>
        {
            SharedScope!.Set("qenex_trigger_variable_id", trigger.VariableId);
            SharedScope.Set("qenex_trigger_variable_name", variableName);
            SharedScope.Set("qenex_trigger_old_value", oldValue);
            SharedScope.Set("qenex_trigger_new_value", newValue);
            SharedScope.Set("qenex_trigger_delta", delta);
        });

        if (executed && observedCallPeriod.HasValue)
        {
            UpdateScriptOverloadGuard(
                overloadKey,
                script,
                observedCallPeriod.Value,
                "OnValueChanged script");
        }
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

    private bool TryEnterObservedScriptCall(string overloadKey, out TimeSpan? observedCallPeriod)
    {
        var nowUtc = DateTime.UtcNow;
        lock (scriptOverloadStateLock)
        {
            var state = GetScriptOverloadState(overloadKey);
            observedCallPeriod = state.LastCallUtc.HasValue
                ? nowUtc - state.LastCallUtc.Value
                : null;
            state.LastCallUtc = nowUtc;

            if (state.SkipCallsRemaining <= 0)
            {
                return true;
            }

            state.SkipCallsRemaining--;
            return false;
        }
    }

    private bool TrySkipScriptCall(string overloadKey)
    {
        lock (scriptOverloadStateLock)
        {
            var state = GetScriptOverloadState(overloadKey);
            if (state.SkipCallsRemaining <= 0)
            {
                return false;
            }

            state.SkipCallsRemaining--;
            return true;
        }
    }

    private void UpdateScriptOverloadGuard(
        string overloadKey,
        IScriptBase script,
        TimeSpan callPeriod,
        string executionSource)
    {
        var skipCalls = CalculateScriptOverloadSkipCount(script.LastExecutionDurationMs, callPeriod);
        if (skipCalls <= 0)
        {
            return;
        }

        var nowUtc = DateTime.UtcNow;
        bool shouldLogWarning;
        lock (scriptOverloadStateLock)
        {
            var state = GetScriptOverloadState(overloadKey);
            state.SkipCallsRemaining = Math.Max(state.SkipCallsRemaining, skipCalls);
            shouldLogWarning = nowUtc - state.LastWarningUtc >= ScriptOverloadWarningInterval;
            if (shouldLogWarning)
            {
                state.LastWarningUtc = nowUtc;
            }
        }

        if (!shouldLogWarning)
        {
            return;
        }

        var periodMs = callPeriod.TotalMilliseconds;
        var durationRatio = script.LastExecutionDurationMs / periodMs * 100;
        logger?.Log(
            LogLevel.Warn,
            $"{executionSource} \"{script.FileName}\" took {script.LastExecutionDurationMs:0.0} ms ({durationRatio:0}% of {periodMs:0.0} ms call period). It exceeds 70% of the call period; skipping next {skipCalls} scheduled call(s).");
    }

    private static int CalculateScriptOverloadSkipCount(double executionDurationMs, TimeSpan callPeriod)
    {
        if (executionDurationMs <= 0 || callPeriod <= TimeSpan.Zero)
        {
            return 0;
        }

        var allowedDurationMs = callPeriod.TotalMilliseconds * ScriptOverloadWarningRatio;
        if (allowedDurationMs <= 0 || executionDurationMs <= allowedDurationMs)
        {
            return 0;
        }

        return Math.Max(1, (int)Math.Ceiling(executionDurationMs / allowedDurationMs) - 1);
    }

    private ScriptCallOverloadState GetScriptOverloadState(string overloadKey)
    {
        if (!scriptOverloadStates.TryGetValue(overloadKey, out var state))
        {
            state = new ScriptCallOverloadState();
            scriptOverloadStates[overloadKey] = state;
        }

        return state;
    }

    private void ClearScriptOverloadState()
    {
        lock (scriptOverloadStateLock)
        {
            scriptOverloadStates.Clear();
        }
    }

    private static string GetScriptOverloadKey(string executionSource, IScriptBase script)
    {
        return $"{executionSource}:{script.FileName}";
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
            var stopTask = Task.WhenAll(periodicScriptTasks);
            if (await WaitForTaskCompletionAsync(stopTask, StopWaitTimeout, CancellationToken.None))
            {
                await stopTask;
                return;
            }

            ObserveBackgroundTask(stopTask, "periodic scripts after stop request");
            logger?.Log(LogLevel.Warn, $"Periodic scripts did not stop within {StopWaitTimeout.TotalMilliseconds:0} ms.");
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
        using var cancellationRegistration = ct.Register(static state => ((PeriodicTimer)state!).Dispose(), timer);
        var overloadKey = GetScriptOverloadKey("periodic", script);

        try
        {
            while (!ct.IsCancellationRequested && await timer.WaitForNextTickAsync())
            {
                if (TrySkipScriptCall(overloadKey))
                {
                    continue;
                }

                var executed = await ExecuteScriptAsync(script, GetScriptExecutionOptions(script), ct);
                if (executed)
                {
                    UpdateScriptOverloadGuard(
                        overloadKey,
                        script,
                        interval,
                        "Periodic script");
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
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

    private ScriptExecutionOptions GetScriptExecutionOptions(IScriptBase script, TimeSpan? defaultTimeout = null)
    {
        TimeSpan? timeout = defaultTimeout;
        if (script.TimeoutMs > 0)
        {
            timeout = TimeSpan.FromMilliseconds(script.TimeoutMs);
        }
        else if (script.TimeoutMs < 0)
        {
            logger?.Log(LogLevel.Warn, $"Script \"{script.FileName}\" has invalid timeout setting \"{script.TimeoutMs}\".");
        }

        return new ScriptExecutionOptions(script.Blocking, timeout);
    }

    private void ResetStopState()
    {
        if (HasAbandonedExecutions)
        {
            throw new InvalidOperationException("Abandoned scripting context cannot be restarted.");
        }

        isStopping = false;
        suppressLateExecutionOutput = false;
        if (!activeExecutionCts.IsCancellationRequested)
        {
            return;
        }

        activeExecutionCts.Dispose();
        activeExecutionCts = new CancellationTokenSource();
    }

    private async Task RunPythonFactoryActionAsync(
        Action action,
        string operationName,
        TimeSpan timeout,
        CancellationToken ct)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(timeout);

        var actionTask = pythonFactory.StartNew(action, timeoutCts.Token);
        if (await WaitForTaskCompletionAsync(actionTask, timeout, ct))
        {
            await AwaitExecutionTaskAsync(actionTask, ct);
            return;
        }

        await timeoutCts.CancelAsync();
        ObserveBackgroundTask(actionTask, operationName);
        logger?.Log(LogLevel.Warn, $"{operationName} did not finish within {timeout.TotalMilliseconds:0} ms.");
    }

    private static async Task<bool> WaitForTaskCompletionAsync(Task task, TimeSpan timeout, CancellationToken ct)
    {
        if (task.IsCompleted)
        {
            return true;
        }

        var delayTask = Task.Delay(timeout, ct);
        return await Task.WhenAny(task, delayTask) == task;
    }

    private static async Task AwaitExecutionTaskAsync(Task task, CancellationToken ct)
    {
        try
        {
            await task;
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void MarkScriptInterrupted(IScriptBase script, string message)
    {
        if (!TryMarkScriptInterrupted(script))
        {
            return;
        }

        script.RunState = ScriptRunState.Faulted;
        logger?.Log(LogLevel.Warn, message);
        OnScriptExecuted(script);
    }

    private bool TryMarkScriptInterrupted(IScriptBase script)
    {
        bool added;
        lock (executionStateLock)
        {
            HasAbandonedExecutions = true;
            suppressLateExecutionOutput = true;
            added = interruptedScripts.Add(script);
            notifiedInterruptedScripts.Add(script);
        }

        DisableOutputWriters();
        return added;
    }

    private bool TryReserveScriptExecution(IScriptBase script)
    {
        lock (executionStateLock)
        {
            return scheduledScripts.Add(script);
        }
    }

    private void ReleaseScriptExecutionWhenCompleted(Task task, IScriptBase script)
    {
        _ = task.ContinueWith(
            _ =>
            {
                lock (executionStateLock)
                {
                    scheduledScripts.Remove(script);
                }
            },
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    private bool ShouldSuppressScriptResult(IScriptBase script)
    {
        lock (executionStateLock)
        {
            return interruptedScripts.Remove(script) || suppressLateExecutionOutput;
        }
    }

    private bool ShouldSuppressScriptNotification(IScriptBase script)
    {
        lock (executionStateLock)
        {
            return notifiedInterruptedScripts.Remove(script) || suppressLateExecutionOutput;
        }
    }

    private void ObserveBackgroundTask(Task task, string operationName)
    {
        _ = task.ContinueWith(
            completedTask =>
            {
                if (completedTask.Exception != null && !ShouldSuppressLateExecutionOutput())
                {
                    logger?.Log(LogLevel.Error, $"{operationName} failed after stop returned: {completedTask.Exception.GetBaseException().Message}");
                }
            },
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted,
            TaskScheduler.Default);
    }

    private void MarkScheduledExecutionsInterruptedOnStop()
    {
        List<IScriptBase> scriptsToNotify;
        lock (executionStateLock)
        {
            if (scheduledScripts.Count == 0)
            {
                return;
            }

            HasAbandonedExecutions = true;
            suppressLateExecutionOutput = true;
            scriptsToNotify = scheduledScripts.ToList();
            foreach (var script in scriptsToNotify)
            {
                interruptedScripts.Add(script);
                notifiedInterruptedScripts.Add(script);
                script.RunState = ScriptRunState.Faulted;
            }
        }

        DisableOutputWriters();
        logger?.Log(LogLevel.Warn, $"Scripting context stopped with {scriptsToNotify.Count} active script execution(s); abandoning context.");
        foreach (var script in scriptsToNotify)
        {
            OnScriptExecuted(script);
        }
    }

    private bool ShouldSuppressLateExecutionOutput()
    {
        lock (executionStateLock)
        {
            return suppressLateExecutionOutput;
        }
    }

    private void DisableOutputWriters()
    {
        stdoutWriter?.Disable();
        stderrWriter?.Disable();
    }

    private string ExecuteScriptContent(string content)
    {
        SharedScope!.Exec(WrapScriptContent(content));
        using var exceptionText = SharedScope.Get(LastScriptExceptionTextName);
        return exceptionText.As<string>();
    }

    private static string WrapScriptContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return $"""
                {LastScriptExceptionTextName} = ""
                import sys as __qenex_sys
                class __QenexScriptStop(BaseException):
                    pass
                __qenex_script_globals = globals()
                def __qenex_script_stop_trace(frame, event, arg):
                    if frame.f_globals is not __qenex_script_globals:
                        return None
                    if event == "line" and {ScriptStopTokenName}.IsStopRequested():
                        raise __QenexScriptStop()
                    return __qenex_script_stop_trace
                __qenex_previous_trace = __qenex_sys.gettrace()
                try:
                    __qenex_sys.settrace(__qenex_script_stop_trace)
                    __qenex_sys._getframe().f_trace = __qenex_script_stop_trace
                    pass
                except __QenexScriptStop:
                    {LastScriptExceptionTextName} = "Script execution was stopped."
                except BaseException:
                    import traceback as __qenex_traceback
                    {LastScriptExceptionTextName} = __qenex_traceback.format_exc()
                finally:
                    __qenex_sys.settrace(__qenex_previous_trace)
                """;
        }

        var builder = new StringBuilder();
        builder.AppendLine($"{LastScriptExceptionTextName} = \"\"");
        builder.AppendLine("import sys as __qenex_sys");
        builder.AppendLine("class __QenexScriptStop(BaseException):");
        builder.AppendLine("    pass");
        builder.AppendLine("__qenex_script_globals = globals()");
        builder.AppendLine("def __qenex_script_stop_trace(frame, event, arg):");
        builder.AppendLine("    if frame.f_globals is not __qenex_script_globals:");
        builder.AppendLine("        return None");
        builder.AppendLine($"    if event == \"line\" and {ScriptStopTokenName}.IsStopRequested():");
        builder.AppendLine("        raise __QenexScriptStop()");
        builder.AppendLine("    return __qenex_script_stop_trace");
        builder.AppendLine("__qenex_previous_trace = __qenex_sys.gettrace()");
        builder.AppendLine("try:");
        builder.AppendLine("    __qenex_sys.settrace(__qenex_script_stop_trace)");
        builder.AppendLine("    __qenex_sys._getframe().f_trace = __qenex_script_stop_trace");

        foreach (var line in content.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n'))
        {
            builder.Append("    ");
            builder.AppendLine(line);
        }

        builder.AppendLine("except __QenexScriptStop:");
        builder.AppendLine($"    {LastScriptExceptionTextName} = \"Script execution was stopped.\"");
        builder.AppendLine("except BaseException:");
        builder.AppendLine("    import traceback as __qenex_traceback");
        builder.AppendLine($"    {LastScriptExceptionTextName} = __qenex_traceback.format_exc()");
        builder.AppendLine("finally:");
        builder.AppendLine("    __qenex_sys.settrace(__qenex_previous_trace)");
        return builder.ToString();
    }
    
    #endregion

    private void OnScriptExecuted(IScriptBase script)
    {
        ScriptExecuted?.Invoke(this, new ScriptExecutedEventArgs(script));
    }
}

internal readonly record struct ScriptExecutionOptions(bool Blocking, TimeSpan? Timeout);

internal sealed class ScriptCallOverloadState
{
    public DateTime? LastCallUtc { get; set; }
    public int SkipCallsRemaining { get; set; }
    public DateTime LastWarningUtc { get; set; } = DateTime.MinValue;
}

public sealed class ScriptStopToken(CancellationToken cancellationToken)
{
    public bool IsStopRequested()
    {
        try
        {
            return cancellationToken.IsCancellationRequested;
        }
        catch (ObjectDisposedException)
        {
            return true;
        }
    }
}

public class ScriptExecutedEventArgs(IScriptBase script) : EventArgs
{
    public IScriptBase Script { get; } = script;
}

using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Threading;
using Qenex.QLibs.QUI;
using Qenex.QSuite.LogSystems.LogSystem;

namespace Qenex.QInsight.ViewModels;

public class ScriptLogsViewModel : ViewModelBase, ILogSubscriber
{
    private const int MaxQueuedLogLines = 5000;
    private const int MaxFlushLogLines = 500;
    private const int MaxLogTextLength = 250000;
    private static readonly TimeSpan FlushInterval = TimeSpan.FromMilliseconds(100);

    private readonly Dispatcher dispatcher;
    private readonly StringBuilder logBuilder;
    private readonly Queue<string> pendingLogLines;
    private readonly object pendingLogLinesLock = new();
    private readonly DispatcherTimer flushTimer;
    private int droppedLogLines;
    
    public ScriptLogsViewModel(EventAggregator ea) : base(ea)
    {
        dispatcher = Dispatcher.CurrentDispatcher;
        EventAggregator.SubscribeAction<LogMessage>(Log);
        logBuilder = new StringBuilder();
        LogText = string.Empty;
        pendingLogLines = new Queue<string>();
        flushTimer = new DispatcherTimer(DispatcherPriority.Background, dispatcher)
        {
            Interval = FlushInterval
        };
        flushTimer.Tick += (_, _) => FlushPendingLogLines();
        
        ClearLogCommand = new RelayCommand<object>(_ =>
        {
            ClearLog();
        });
    }

    #region Properties

    public string LogText { get => field;
        set { field = value; OnPropertyChanged(); }
    }
    public RelayCommand<object> ClearLogCommand { get; set; }
    
    
    #endregion
    
    #region ViewModelBase implementation

    public override string Header { get; set; } = "Script-Logs";
    public override string Name { get; set; } = "ScriptLogsViewModel";
    public override DockingPosition DockPosition { get; set; } = DockingPosition.Bottom;
    public override bool IsDocument => false;
    

    #endregion
    
    #region ILogger implementation

    public void ClearLog()
    {
        if (dispatcher.CheckAccess())
        {
            ClearLogCore();
        }
        else
        {
            dispatcher.BeginInvoke(ClearLogCore);
        }
    }

    public void Log(ILogMessage message)
    {
        if (message.Level != LogLevel.Script) return;

        EnqueueLogLine($"{message.Timestamp:HH:mm:ss.fff}   {message.Message}");
    }

    public Task LogAsync(ILogMessage message, CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
        {
            return Task.FromCanceled(ct);
        }

        Log(message);
        return Task.CompletedTask;
    }

    private void EnqueueLogLine(string logLine)
    {
        lock (pendingLogLinesLock)
        {
            if (pendingLogLines.Count >= MaxQueuedLogLines)
            {
                droppedLogLines++;
            }
            else
            {
                pendingLogLines.Enqueue(logLine);
            }
        }

        if (dispatcher.CheckAccess())
        {
            EnsureFlushTimerRunning();
        }
        else
        {
            dispatcher.BeginInvoke(EnsureFlushTimerRunning, DispatcherPriority.Background);
        }
    }

    private void EnsureFlushTimerRunning()
    {
        if (!flushTimer.IsEnabled)
        {
            flushTimer.Start();
        }
    }

    private void FlushPendingLogLines()
    {
        List<string> linesToFlush = [];
        var droppedLinesToReport = 0;
        var hasMoreLines = false;

        lock (pendingLogLinesLock)
        {
            droppedLinesToReport = droppedLogLines;
            droppedLogLines = 0;

            while (linesToFlush.Count < MaxFlushLogLines && pendingLogLines.Count > 0)
            {
                linesToFlush.Add(pendingLogLines.Dequeue());
            }

            hasMoreLines = pendingLogLines.Count > 0 || droppedLogLines > 0;
        }

        if (droppedLinesToReport > 0)
        {
            logBuilder.AppendLine($"{DateTime.Now:HH:mm:ss.fff}   Script log output is too fast; skipped {droppedLinesToReport} line(s).");
        }

        foreach (var line in linesToFlush)
        {
            logBuilder.AppendLine(line);
        }

        TrimLogText();
        LogText = logBuilder.ToString();

        if (!hasMoreLines)
        {
            lock (pendingLogLinesLock)
            {
                if (pendingLogLines.Count == 0 && droppedLogLines == 0)
                {
                    flushTimer.Stop();
                }
            }
        }
    }

    private void TrimLogText()
    {
        if (logBuilder.Length <= MaxLogTextLength)
        {
            return;
        }

        var removeLength = logBuilder.Length - MaxLogTextLength;
        while (removeLength < logBuilder.Length && logBuilder[removeLength] != '\n')
        {
            removeLength++;
        }

        if (removeLength < logBuilder.Length)
        {
            removeLength++;
        }

        logBuilder.Remove(0, removeLength);
    }

    private void ClearLogCore()
    {
        lock (pendingLogLinesLock)
        {
            pendingLogLines.Clear();
            droppedLogLines = 0;
        }

        flushTimer.Stop();
        logBuilder.Clear();
        LogText = string.Empty;
    }

    #endregion
}

using System.Collections.ObjectModel;
using System.Windows.Threading;
using Qenex.QLibs.QUI;
using Qenex.QLibs.QUI.Wpf;
using Qenex.QSuite.LogSystems.LogSystem;

namespace Qenex.QInsight.ViewModels;

public class LogsViewModel : ViewModelBase, ILogSubscriber
{
    // Everything (except Script) is collected up to this cap so debug detail can be
    // revealed retroactively; the oldest messages fall off first.
    private const int MaxLogMessages = 5000;

    private readonly Dispatcher dispatcher;
    private readonly List<ILogMessage> allMessages = [];

    public LogsViewModel(EventAggregator ea) : base(ea)
    {
        dispatcher = Dispatcher.CurrentDispatcher;
        EventAggregator.SubscribeAction<LogMessage>(Log);
        LogMessages = [];

        ClearLogCommand = new RelayCommand<object>(_ => ClearLog());
	}

    #region Properties

    /// <summary>
    /// Messages currently shown in the Logs panel (filtered by <see cref="ShowDebugMessages"/>).
    /// </summary>
    public ObservableCollection<ILogMessage> LogMessages { get; set; }
    public RelayCommand<object> ClearLogCommand { get; set; }

    /// <summary>
    /// Trace/Debug rows are collected but hidden until the user enables them in General
    /// preferences — flipping the switch also reveals the already-recorded detail.
    /// </summary>
    public bool ShowDebugMessages
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            OnPropertyChanged();
            if (dispatcher.CheckAccess())
            {
                RebuildVisibleMessages();
            }
            else
            {
                dispatcher.BeginInvoke(RebuildVisibleMessages);
            }
        }
    }

    #endregion

    #region ViewModelBase implementation

    public override string Header { get; set; } = "Logs";
    public override string Name { get; set; } = "LogsViewModel";
    public override DockingPosition DockPosition { get; set; } = DockingPosition.Bottom;
    public override bool IsDocument => false;


    #endregion

    #region ILogger implementation

    public void ClearLog()
    {
        if (dispatcher.CheckAccess())
        {
            ClearAllMessages();
        }
        else
        {
            dispatcher.BeginInvoke(ClearAllMessages);
        }
    }

    public void Log(ILogMessage message)
    {
        if (message.Level == LogLevel.Script) return;
        if (dispatcher.CheckAccess())
        {
            Append(message);
        }
        else
        {
            dispatcher.BeginInvoke(() => Append(message));
        }
    }

    public Task LogAsync(ILogMessage message, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    #endregion

    #region Private methods

    private void Append(ILogMessage message)
    {
        allMessages.Add(message);
        while (allMessages.Count > MaxLogMessages)
        {
            var oldest = allMessages[0];
            allMessages.RemoveAt(0);
            LogMessages.Remove(oldest);
        }

        if (IsVisible(message))
        {
            LogMessages.Add(message);
        }
    }

    private bool IsVisible(ILogMessage message)
    {
        return ShowDebugMessages || message.Level >= LogLevel.Info;
    }

    private void RebuildVisibleMessages()
    {
        LogMessages.Clear();
        foreach (var message in allMessages.Where(IsVisible))
        {
            LogMessages.Add(message);
        }
    }

    private void ClearAllMessages()
    {
        allMessages.Clear();
        LogMessages.Clear();
    }

    #endregion
}

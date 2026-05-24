using System.Collections.ObjectModel;
using System.Windows.Threading;
using Qenex.QLibs.QUI;
using Qenex.QLibs.QUI.Wpf;
using Qenex.QSuite.LogSystems.LogSystem;

namespace Qenex.QInsight.ViewModels;

public class LogsViewModel : ViewModelBase, ILogSubscriber
{
    private readonly Dispatcher dispatcher;
    public LogsViewModel(EventAggregator ea) : base(ea)
    {
        dispatcher = Dispatcher.CurrentDispatcher;
        EventAggregator.SubscribeAction<LogMessage>(Log);
        LogMessages = [];
        
        ClearLogCommand = new RelayCommand<object>(_ => ClearLog());
	}

    #region Properties

    public ObservableCollection<ILogMessage> LogMessages { get; set; }
    public RelayCommand<object> ClearLogCommand { get; set; }
    
    
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
            LogMessages.Clear();
        }
        else
        {
            dispatcher.BeginInvoke(LogMessages.Clear);
        }
    }

    public void Log(ILogMessage message)
    {
        if (message.Level == LogLevel.Script) return;
        if (dispatcher.CheckAccess())
        {
            LogMessages.Add(message);
        }
        else
        {
            dispatcher.BeginInvoke(() => LogMessages.Add(message));
        }
    }

    public Task LogAsync(ILogMessage message, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    #endregion
}

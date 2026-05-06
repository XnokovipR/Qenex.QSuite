using System.Collections.ObjectModel;
using System.Windows.Threading;
using Qenex.QLibs.QUI;
using Qenex.QSuite.LogSystems.LogSystem;

namespace Qenex.QInsight.ViewModels;

public class ScriptLogsViewModel : ViewModelBase, ILogSubscriber
{
    private readonly Dispatcher dispatcher;
    
    public ScriptLogsViewModel(EventAggregator ea) : base(ea)
    {
        dispatcher = Dispatcher.CurrentDispatcher;
        EventAggregator.SubscribeAction<LogMessage>(Log);
        LogMessages = [];
        
        ClearLogCommand = new RelayCommand<object>(_ => LogMessages.Clear());
    }

    #region Properties

    public ObservableCollection<ILogMessage> LogMessages { get; set; }
    public RelayCommand<object> ClearLogCommand { get; set; }
    
    
    #endregion
    
    #region ViewModelBase implementation

    public override string Header { get; set; } = "Script-Logs";
    public override string Name { get; set; } = "ScriptLogsViewModel";
    public override DockingPosition DockPosition { get; set; } = DockingPosition.Bottom;
    public override bool IsDocument => false;
    

    #endregion
    
    #region ILogger implementation

    public void Log(ILogMessage message)
    {
        if (message.Level != LogLevel.Script) return;
        
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
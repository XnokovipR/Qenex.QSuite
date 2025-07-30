using System.Collections.ObjectModel;
using Qenex.QLibs.QUI;
using Qenex.QLibs.QUI.Wpf;
using Qenex.QSuite.LogSystems.LogSystem;

namespace Qenex.QInsight.ViewModels;

public class LogsViewModel : ViewModelBase, ILogSubscriber
{
    public LogsViewModel(EventAggregator ea) : base(ea)
    {
        EventAggregator.SubscribeAction<LogMessage>(Log);
        LogMessages = [];
	}

    #region Properties

    public ObservableCollection<ILogMessage> LogMessages { get; set; }
    
    #endregion
    
    #region ViewModelBase implementation

    public override string Header { get; set; } = "Logs";
    public override string Name { get; set; } = "LogsViewModel";
    public override DockingPosition DockPosition { get; set; } = DockingPosition.Bottom;
    public override bool IsDocument => false;
    
    public override void Exit()
    {
    }

    #endregion
    
    #region ILogger implementation

    public void Log(ILogMessage message)
    {
        LogMessages.Add(message);
    }

    public Task LogAsync(ILogMessage message, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    #endregion
}
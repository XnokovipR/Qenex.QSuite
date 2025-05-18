using System.Collections.ObjectModel;
using Qenex.QLibs.QUI;
using Qenex.QLibs.QUI.TelerikDocking;
using Qenex.QSuite.LogSystems.LogSystem;

namespace Qenex.QSuite.ViewModels;

public class LogViewModel : ViewModelBase, ILogSubscriber
{
	#region Private fields

	#endregion

	#region Constructors
	public LogViewModel(EventAggregator ea) : base(ea)
    {
        EventAggregator.SubscribeAction<LogMessage>(Log);
        LogMessages = new ObservableCollection<ILogMessage>();
    }
	#endregion

	#region Properties
	public ObservableCollection<ILogMessage> LogMessages { get; set; }
    public RelayCommand<object> ClearLogCommand => new RelayCommand<object>(o => LogMessages.Clear());

    #endregion
    
    #region BaseViewModel implementation
    
    public override string Identifier => $"LogViewModel:{Guid.NewGuid().ToString()}";
    public override string Header { get => "Logs"; set { } }
    public override string Name { get => "LogViewModel"; set { } }
    public override bool IsDocument => false;

    public override DockingPosition DockPosition { get => DockingPosition.Bottom; set { } }

	public override void Exit() { }

	#endregion

	#region Public methods

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

    #endregion
}
using System.Collections.ObjectModel;
using Qenex.QLibs.QUI;
using Qenex.QSuite.LogSystems.LogSystem;
using Syncfusion.Windows.Tools.Controls;

namespace Qenex.QSuite.ViewModels;

public class LogViewModel : ViewModelBase, ILogSubscriber
{
    public LogViewModel(EventAggregator ea) : base(ea)
    {
        EventAggregator.SubscribeAction<LogMessage>(Log);
        LogMessages = new ObservableCollection<ILogMessage>();
    }
 
    #region Properties
    public ObservableCollection<ILogMessage> LogMessages { get; set; }
    public RelayCommand<object> ClearLogCommand => new RelayCommand<object>(o => LogMessages.Clear());

    #endregion
    
    #region BaseViewModel implementation
    
    public override string Identifier => $"LogViewModel:{Guid.NewGuid().ToString()}";
    public override string Header { get => "Logs"; set { } }

    public override string Name { get => "LogViewModel"; set { } }

    public override DockSide DockingPosition { get => DockSide.Bottom; set { } }

    public override DockState DockState { get; set; }
    public override bool CanMaximize => false;

    public override bool IsDocument => false;
    public override bool CanClose => false;
    public override bool CanSerialize => true;

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
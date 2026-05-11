using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Threading;
using Qenex.QLibs.QUI;
using Qenex.QSuite.LogSystems.LogSystem;

namespace Qenex.QInsight.ViewModels;

public class ScriptLogsViewModel : ViewModelBase, ILogSubscriber
{
    private readonly Dispatcher dispatcher;
    private readonly StringBuilder logBuilder;
    
    public ScriptLogsViewModel(EventAggregator ea) : base(ea)
    {
        dispatcher = Dispatcher.CurrentDispatcher;
        EventAggregator.SubscribeAction<LogMessage>(Log);
        logBuilder = new StringBuilder();
        
        ClearLogCommand = new RelayCommand<object>(_ =>
        {
            logBuilder.Clear();
            LogText = string.Empty;
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

    public void Log(ILogMessage message)
    {
        if (message.Level != LogLevel.Script) return;
        
        if (dispatcher.CheckAccess())
        {
            AddLog(message);
        }
        else
        {
            dispatcher.BeginInvoke(() => AddLog(message));
        }
    }

    public Task LogAsync(ILogMessage message, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    private void AddLog(ILogMessage msg)
    {
        logBuilder.AppendLine($"{msg.Timestamp:HH:mm:ss.fff}   {msg.Message}");
        LogText = logBuilder.ToString();
    }

    #endregion
}
using System.Windows;
using Qenex.QSuite.Common.WpfComm;
using Qenex.QSuite.Controls.Control;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Qenex.QSuite.Protocols.Protocol;
using Qenex.QSuite.Variables.QVariables;

namespace Qenex.QSuite.Controls.SignalControl.ViewModels;

public class SignalControlViewModel : ControlBase
{
	private DateTime previousUpdateTime = DateTime.MinValue;
	
    public SignalControlViewModel()
    {
		Width = 120;
		Height = 60;
		
		VariableLabel = "----------";
		VariableValue = "";
		VariableUnit = "-";
    }

    #region Properties

    public string VariableLabel { get; set { field = value; OnPropertyChanged(); } }

    public string VariableValue { get; set { field = value; OnPropertyChanged(); } }

    public string VariableUnit { get; set { field = value; OnPropertyChanged(); } }
    
    public int RefreshTime
    { 
	    get;
	    set
	    {
		    if (value < 0) value = 0;
		    field = value; OnPropertyChanged();
	    } 
    } = 250;
    
    #endregion

    #region Derived properties

    public override string ControlName => "SignalControl";
    public override string Label => "Single-Signal";
    public override BitmapImage Icon => ImageGetter.GetBitmapImage("Icons/SingleSignalControl.png");
    public override string Description => "Signal control for displaying a single signal.";
    
    #endregion

    #region Public methods

    public override async Task UpdateVariableValueAsync(IVariableBase protVariable)
    {
	    var dataValue = protVariable switch
	    {
		    ScalarVariable scVar => scVar.Values.ToString(),
		    StringVariable stVar => stVar.Values
	    };
	    
	    if (dataValue == null ) return;
	    if ((protVariable.Timestamp - previousUpdateTime).TotalMilliseconds < RefreshTime) return;
	    previousUpdateTime = protVariable.Timestamp;
	    
	    _ = Application.Current.Dispatcher.BeginInvoke(() =>
	    {
		    VariableValue = dataValue;
	    });
	    
    }

    public override void BindVariable(IVariableBase protVariable)
    {
	    Variables.Add(protVariable);
	    VariableLabel = protVariable.Label;
	    VariableUnit = protVariable is ScalarVariable variable ? variable.Values.ValPresentation.Unit : string.Empty;
	    VariableValue = string.Empty;
    }

    #endregion
}
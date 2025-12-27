using Qenex.QSuite.Common.WpfComm;
using Qenex.QSuite.Controls.Control;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Qenex.QSuite.Protocols.Protocol;
using Qenex.QSuite.Variables.QVariables;

namespace Qenex.QSuite.Controls.SignalControl.ViewModels;

public class SignalControlViewModel : ControlBase
{
    public SignalControlViewModel()
    {
		Width = 100;
		Height = 60;
		
		VariableLabel = "----------";
		VariableValue = "";
		VariableUnit = "";
    }

    #region Properties

    public string VariableLabel { get; set { field = value; OnPropertyChanged(); } }

    public string VariableValue { get; set { field = value; OnPropertyChanged(); } }

    public string VariableUnit { get; set { field = value; OnPropertyChanged(); } }
    
    #endregion

    #region Derived properties

    public override string ControlName => "SignalControl";
    public override string Label => "Single-Signal";
    public override BitmapImage Icon => ImageGetter.GetBitmapImage("Icons/SingleSignalControl.png");
    public override string Description => "Signal control for displaying a single signal.";
    
    #endregion

    #region Public methods

    public override Task UpdateVariableValueAsync(IVariableBase protVariable)
    {
	    VariableValue = protVariable switch
	    {
		    ScalarVariable scVar => scVar.Values.ToString(),
		    StringVariable stVar => stVar.Values
	    };

	    return Task.CompletedTask;
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
using System.Runtime.Serialization;
using System.Windows;
using Qenex.QSuite.Common.WpfComm;
using Qenex.QSuite.Controls.Control;
using System.Windows.Media.Imaging;
using Qenex.QSuite.Variables.QVariables;

namespace Qenex.QSuite.Controls.SignalControl.ViewModels;

[DataContract]
public class SignalControlViewModel : ControlBase
{
	private DateTime previousUpdateTime = DateTime.MinValue;
	private double prevValue;
	
    public SignalControlViewModel()
    {
		Width = 140;
		Height = 60;
		
		VariableLabel = "----------";
		VariableValue = "";
		VariableUnit = "-";
    }

    #region Properties

    [IgnoreDataMember]
    public string VariableLabel { get; set { field = value; OnPropertyChanged(); } }

    [IgnoreDataMember]
    public string VariableValue { get; set { field = value; OnPropertyChanged(); } }

    [IgnoreDataMember]
    public string VariableUnit { get; set { field = value; OnPropertyChanged(); } }
    
    [DataMember]
    public int RefreshTime
    { 
	    get;
	    set
	    {
		    if (value < 0) value = 0;
		    field = value; OnPropertyChanged();
	    } 
    } = 250;
    
    [DataMember]
    public int DeathBendPercentage
    { 
	    get;
	    set
	    {
		    if (value < 0) value = 0;
		    field = value; OnPropertyChanged();
	    } 
    } = 5;
    
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
		    ScalarVariable scVar => scVar.GetPresentationText(),
		    StringVariable stVar => stVar.Values
	    };
	    
	    if (dataValue == null ) return;
	    if (protVariable.Timestamp < previousUpdateTime)
	    {
		    previousUpdateTime = DateTime.MinValue;
		    prevValue = 0;
	    }

	    if ((protVariable.Timestamp - previousUpdateTime).TotalMilliseconds < RefreshTime) return;
	    previousUpdateTime = protVariable.Timestamp;

	    if (double.TryParse(dataValue, out double doubleValue))
	    {
		    if (Math.Abs((doubleValue - prevValue) / prevValue * 100) < DeathBendPercentage) return;
		    prevValue = doubleValue;
	    }
	    
	    _ = Application.Current.Dispatcher.BeginInvoke(() =>
	    {
		    VariableValue = dataValue;
	    });
	    
    }

    public override void BindVariable(IVariableBase protVariable)
    {
	    RememberVariableBinding(protVariable);
	    var existingVariable = Variables.FirstOrDefault(v => v.Equals(protVariable));
	    if (existingVariable != null)
	    {
		    return;
	    }

	    Variables.Add(protVariable);
	    VariableLabel = protVariable.Label;
	    VariableUnit = protVariable is ScalarVariable variable ? variable.Values.ValPresentation.Unit : string.Empty;
	    VariableValue = string.Empty;
	    previousUpdateTime = DateTime.MinValue;
	    prevValue = 0;
    }

    public override void RefreshVariableBinding(IVariableBase variable)
    {
	    base.RefreshVariableBinding(variable);

	    if (!IsVariableBound(variable))
	    {
		    return;
	    }

	    VariableLabel = variable.Label;
	    VariableUnit = variable is ScalarVariable scalarVariable
		    ? scalarVariable.Values.ValPresentation.Unit
		    : string.Empty;
    }

    protected override void OnEditToRun()
    {
	    VariableValue = string.Empty;
	    previousUpdateTime = DateTime.MinValue;
	    prevValue = 0;
    }

    [OnDeserialized]
    private void OnDeserialized(StreamingContext context)
    {
	    VariableLabel ??= "----------";
	    VariableValue ??= "----------";
	    VariableUnit ??= string.Empty;
	    Variables ??= [];
	    LinkedVariables ??= [];
    }

    #endregion
}

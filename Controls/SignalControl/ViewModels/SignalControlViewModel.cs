using System.Globalization;
using System.Runtime.Serialization;
using System.Windows;
using Qenex.QLibs.QUI;
using Qenex.QSuite.Common.WpfComm;
using Qenex.QSuite.Controls.Control;
using System.Windows.Media.Imaging;
using Qenex.QSuite.Variables.QVariables;

namespace Qenex.QSuite.Controls.SignalControl.ViewModels;

[DataContract]
public class SignalControlViewModel : ControlBase, IVariableWriteControl
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

    #region Write mode (IVariableWriteControl)

    [IgnoreDataMember]
    public Func<IVariableBase, bool>? CanWriteVariableProvider { get; set; }

    [IgnoreDataMember]
    public Func<IVariableBase, double, Task<bool>>? WriteVariableEngValueAsync { get; set; }

    [DataMember]
    public bool IsWriteMode
    {
	    get;
	    set
	    {
		    field = value;
		    OnPropertyChanged();
		    OnPropertyChanged(nameof(IsWriteActive));
		    if (value)
		    {
			    PrefillEditValue();
		    }
	    }
    }

    [IgnoreDataMember]
    public bool CanWrite
    {
	    get;
	    private set
	    {
		    field = value;
		    OnPropertyChanged();
		    OnPropertyChanged(nameof(IsWriteActive));
	    }
    }

    [IgnoreDataMember]
    public bool IsWriteActive => IsWriteMode && CanWrite;

    [IgnoreDataMember]
    public string EditValue { get; set { field = value; OnPropertyChanged(); IsWriteError = false; } }

    [IgnoreDataMember]
    public bool IsWriteError { get; set { field = value; OnPropertyChanged(); } }

    // Lazy kvuli deserializaci (DataContractSerializer nevola konstruktor)
    [IgnoreDataMember]
    public RelayCommand<object> WriteValueCommand => field ??= new RelayCommand<object>(OnWriteValue);

    public void RefreshWriteCapability()
    {
	    var variable = Variables?.FirstOrDefault();
	    CanWrite = variable != null && (CanWriteVariableProvider?.Invoke(variable) ?? false);
    }

    private void OnWriteValue(object parameter)
    {
	    _ = WriteValueAsync();
    }

    private async Task WriteValueAsync()
    {
	    if (!IsWriteActive || !IsRun || WriteVariableEngValueAsync == null)
	    {
		    IsWriteError = true;
		    return;
	    }

	    var variable = Variables?.FirstOrDefault();
	    if (variable == null || !TryParseEditValue(out var engValue))
	    {
		    IsWriteError = true;
		    return;
	    }

	    try
	    {
		    var written = await WriteVariableEngValueAsync(variable, engValue);
		    IsWriteError = !written;
	    }
	    catch
	    {
		    IsWriteError = true;
	    }
    }

    private bool TryParseEditValue(out double engValue)
    {
	    return double.TryParse((EditValue ?? string.Empty).Replace(',', '.'),
		    NumberStyles.Float, CultureInfo.InvariantCulture, out engValue);
    }

    private void PrefillEditValue()
    {
	    EditValue = Variables?.FirstOrDefault() is ScalarVariable scalarVariable
		    ? scalarVariable.GetEngValue().ToString(CultureInfo.InvariantCulture)
		    : string.Empty;
	    IsWriteError = false;
    }

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
	    // Ve write rezimu se displej tohoto controlu zmrazi (komunikace bezi dal,
	    // ostatni controly stejnou promennou zobrazuji normalne)
	    if (IsWriteActive) return;

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
	    if (Variables.Any(v => v.Equals(protVariable)))
	    {
		    return;
	    }

	    // Single-variable control: replace the previous variable (host unsubscribes the old).
	    Variables.Clear();
	    LinkedVariables.Clear();

	    RememberVariableBinding(protVariable);
	    Variables.Add(protVariable);
	    VariableLabel = protVariable.Label;
	    VariableUnit = protVariable is ScalarVariable variable ? variable.Values.ValPresentation.Unit : string.Empty;
	    VariableValue = string.Empty;
	    previousUpdateTime = DateTime.MinValue;
	    prevValue = 0;

	    // Zapisovatelnost se musi prehodnotit pri kazdem (re)bindu
	    RefreshWriteCapability();
	    if (IsWriteMode)
	    {
		    PrefillEditValue();
	    }
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
	    EditValue ??= string.Empty;
    }

    #endregion
}

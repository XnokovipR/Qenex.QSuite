using System.Windows;
using Qenex.QLibs.QUI;
using Qenex.QSuite.Protocols.Protocol;
using Qenex.QSuite.Variables.QVariables;

namespace Qenex.QInsight.ViewModels;

public class ReadOnlyVariablePropertiesViewModel : PropertyChangedBaseWithValidation, IPropertiesViewModel
{
    private readonly IVariableBase variable;
    private readonly ScalarVariable? scalarVariable;

    public ReadOnlyVariablePropertiesViewModel(EventAggregator ea, IProtocolVariable protocolVariable)
    {
        _ = ea;
        variable = protocolVariable.Variable;
        scalarVariable = variable as ScalarVariable;
    }

    public int Id => variable.Id;
    public string Name => variable.Name;
    public string Label => variable.Label;
    public string Description => variable.Description;
    public Visibility ScalarPropertiesVisibility => scalarVariable != null ? Visibility.Visible : Visibility.Collapsed;
    public string Size => scalarVariable?.Size.ToString() ?? string.Empty;
    public string DataType => scalarVariable?.Values.ValueType.ToString() ?? string.Empty;
    public string ValueSize => scalarVariable?.Values.Size.ToString() ?? string.Empty;
    public string Length => scalarVariable?.Values.Length.ToString() ?? string.Empty;
    public string Presentation => scalarVariable?.Values.ValPresentation?.Name ?? string.Empty;

    public void RefreshProperties()
    {
        OnPropertyChanged(nameof(Id));
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Label));
        OnPropertyChanged(nameof(Description));
        OnPropertyChanged(nameof(ScalarPropertiesVisibility));
        OnPropertyChanged(nameof(Size));
        OnPropertyChanged(nameof(DataType));
        OnPropertyChanged(nameof(ValueSize));
        OnPropertyChanged(nameof(Length));
        OnPropertyChanged(nameof(Presentation));
    }
}

using Qenex.QLibs.QUI;
using Qenex.QSuite.Variables.QVariables;

namespace Qenex.QInsight.ViewModels;

public class VariablesPropertiesViewModel : PropertyChangedBaseWithValidation, IPropertiesViewModel
{
    private readonly Func<bool> getIsEditVariableEnabled;
    private readonly Action<bool> setIsEditVariableEnabled;

    public VariablesPropertiesViewModel(
        EventAggregator ea,
        IList<IVariableBase> variables,
        Func<bool> getIsEditVariableEnabled,
        Action<bool> setIsEditVariableEnabled)
    {
        _ = ea;
        _ = variables;
        this.getIsEditVariableEnabled = getIsEditVariableEnabled;
        this.setIsEditVariableEnabled = setIsEditVariableEnabled;
    }

    public bool IsEditVariableEnabled
    {
        get => getIsEditVariableEnabled();
        set
        {
            if (getIsEditVariableEnabled() == value)
            {
                return;
            }

            setIsEditVariableEnabled(value);
            OnPropertyChanged();
        }
    }
}

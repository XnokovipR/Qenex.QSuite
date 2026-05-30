using Qenex.QLibs.QUI;
using Qenex.QSuite.Variables.ValueConversion;

namespace Qenex.QInsight.ViewModels;

public class ConversionsPropertiesViewModel : PropertyChangedBaseWithValidation, IPropertiesViewModel
{
    private readonly Func<bool> getIsEditConversionEnabled;
    private readonly Action<bool> setIsEditConversionEnabled;

    public ConversionsPropertiesViewModel(
        EventAggregator ea,
        IList<IValConversion> conversions,
        Func<bool> getIsEditConversionEnabled,
        Action<bool> setIsEditConversionEnabled)
    {
        _ = ea;
        _ = conversions;
        this.getIsEditConversionEnabled = getIsEditConversionEnabled;
        this.setIsEditConversionEnabled = setIsEditConversionEnabled;
    }

    public bool IsEditConversionEnabled
    {
        get => getIsEditConversionEnabled();
        set
        {
            if (getIsEditConversionEnabled() == value)
            {
                return;
            }

            setIsEditConversionEnabled(value);
            OnPropertyChanged();
        }
    }
}

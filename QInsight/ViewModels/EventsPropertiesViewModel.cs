using Qenex.QLibs.QUI;
using Qenex.QSuite.Variables.VariableEvents;

namespace Qenex.QInsight.ViewModels;

public class EventsPropertiesViewModel : PropertyChangedBaseWithValidation, IPropertiesViewModel
{
    private readonly Func<bool> getIsEditEventEnabled;
    private readonly Action<bool> setIsEditEventEnabled;

    public EventsPropertiesViewModel(
        EventAggregator ea,
        IList<IVarEvent> events,
        Func<bool> getIsEditEventEnabled,
        Action<bool> setIsEditEventEnabled)
    {
        _ = ea;
        _ = events;
        this.getIsEditEventEnabled = getIsEditEventEnabled;
        this.setIsEditEventEnabled = setIsEditEventEnabled;
    }

    public bool IsEditEventEnabled
    {
        get => getIsEditEventEnabled();
        set
        {
            if (getIsEditEventEnabled() == value)
            {
                return;
            }

            setIsEditEventEnabled(value);
            OnPropertyChanged();
        }
    }
}

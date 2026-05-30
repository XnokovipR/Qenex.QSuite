using Qenex.QLibs.QUI;
using Qenex.QSuite.Variables.ValuePresentation;

namespace Qenex.QInsight.ViewModels;

public class PresentationsPropertiesViewModel : PropertyChangedBaseWithValidation, IPropertiesViewModel
{
    private readonly Func<bool> getIsEditPresentationEnabled;
    private readonly Action<bool> setIsEditPresentationEnabled;

    public PresentationsPropertiesViewModel(
        EventAggregator ea,
        IList<IPresentation> presentations,
        Func<bool> getIsEditPresentationEnabled,
        Action<bool> setIsEditPresentationEnabled)
    {
        _ = ea;
        _ = presentations;
        this.getIsEditPresentationEnabled = getIsEditPresentationEnabled;
        this.setIsEditPresentationEnabled = setIsEditPresentationEnabled;
    }

    public bool IsEditPresentationEnabled
    {
        get => getIsEditPresentationEnabled();
        set
        {
            if (getIsEditPresentationEnabled() == value)
            {
                return;
            }

            setIsEditPresentationEnabled(value);
            OnPropertyChanged();
        }
    }
}

using Qenex.QLibs.QUI;
using Qenex.QSuite.Controls.Control;

namespace Qenex.QInsight.ViewModels;

public class ControlPropertiesViewModel : PropertyChangedBaseWithValidation, IPropertiesViewModel
{
    private readonly ControlBase control;

    public ControlPropertiesViewModel(EventAggregator ea, string workspaceName, ControlBase control)
    {
        _ = ea;
        Workspace = workspaceName;
        this.control = control;
    }

    public string Control => WorkspaceViewModel.GetControlDisplayName(control);
    public int Id => control.Id;
    public string Workspace { get; }

    public string Variables => string.Join(", ", control.LinkedVariables.Select(GetVariableName));

    // Linked variable references are "id|namespace|name"; the user knows the name.
    private static string GetVariableName(string variableReference)
    {
        var parts = variableReference.Split('|');
        return parts.Length == 3 ? parts[2] : variableReference;
    }
}

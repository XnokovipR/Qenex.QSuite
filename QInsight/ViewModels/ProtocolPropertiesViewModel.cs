using Qenex.QInsight.Helpers;
using Qenex.QLibs.QUI;
using Qenex.QSuite.Protocols.Protocol;

namespace Qenex.QInsight.ViewModels;

public class ProtocolPropertiesViewModel : PropertyChangedBaseWithValidation, IPropertiesViewModel
{
    private readonly IProtocolBase protocol;

    public ProtocolPropertiesViewModel(EventAggregator ea, IProtocolBase protocol)
    {
        _ = ea;
        this.protocol = protocol;
    }

    public string Name => protocol.Specification.Name;
    public string Label => protocol.Specification.Label;
    public string Description => protocol.Specification.Description;
    public string Version => protocol.Specification.Version.ToDisplayString();
    public string Author => protocol.Specification.Author ?? string.Empty;
    public string Company => protocol.Specification.Company ?? string.Empty;
    public string CreatedOn => protocol.Specification.CreatedOn.ToString("yyyy/MM/dd");
    public bool IsEnabled => protocol.IsEnabled;
    public string State => protocol.State.ToString();
    public string StateMessage => protocol.StateMessage ?? string.Empty;
    public int VariableCount => protocol.Variables.Count;
}

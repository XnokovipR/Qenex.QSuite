using System.Collections.ObjectModel;
using Qenex.QInsight.ViewModels.ModelWrappers;
using Qenex.QLibs.QUI;
using Qenex.QSuite.Protocols.Protocol;

namespace Qenex.QInsight.ViewModels;

public class CommunicatedProtocolsPropertiesViewModel : PropertyChangedBaseWithValidation, IPropertiesViewModel
{
    public CommunicatedProtocolsPropertiesViewModel(EventAggregator ea, IEnumerable<IProtocolBase> protocols)
    {
        _ = ea;
        Protocols = new ObservableCollection<ProtocolWrapper>(
            protocols
                .Where(protocol => !protocol.Specification.Name.Equals("DataLogReplayProtocol", StringComparison.OrdinalIgnoreCase))
                .Select(protocol => new ProtocolWrapper(protocol)));
    }

    public ObservableCollection<ProtocolWrapper> Protocols { get; }

    public void RefreshProtocols()
    {
        foreach (var protocol in Protocols)
        {
            protocol.Refresh();
        }
    }
}

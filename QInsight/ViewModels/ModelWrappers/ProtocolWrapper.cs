using System.Text;
using Qenex.QLibs.QUI;
using Qenex.QSuite.Protocols.Protocol;

namespace Qenex.QInsight.ViewModels.ModelWrappers;

public class ProtocolWrapper(IProtocolBase protocol) : PropertyChangedBase
{
    public IProtocolBase Protocol => protocol;

    public string Label
    {
        get => protocol.Specification.Label;
        set
        {
            if (protocol.Specification.Label == value) return;
            protocol.Specification.Label = value;
            OnPropertyChanged();
        }
    }

    public string ToolTip
    {
        get
        {
            var sb = new StringBuilder();
            sb.Append("Protocol:");
            sb.Append(Environment.NewLine);
            sb.Append($"Label\t{protocol.Specification.Label}");
            sb.Append(Environment.NewLine);
            sb.Append($"Desc.\t{protocol.Specification.Description}");
            sb.Append(Environment.NewLine);
            sb.Append($"Version\t{protocol.Specification.Version}");
            sb.Append(Environment.NewLine);
            sb.Append($"Author\t{protocol.Specification.Author}");
            sb.Append(Environment.NewLine);
            sb.Append($"Co.\t{protocol.Specification.Company}");

            return sb.ToString();
        }
    }

    public bool IsEnabled
    {
        get => protocol.IsEnabled;
        set
        {
            if (protocol.IsEnabled == value) return;
            protocol.IsEnabled = value;
            OnPropertyChanged();
        }
    }

    public void Refresh()
    {
        OnPropertyChanged(nameof(Label));
        OnPropertyChanged(nameof(ToolTip));
        OnPropertyChanged(nameof(IsEnabled));
    }
}

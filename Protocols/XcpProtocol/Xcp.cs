using System.Reflection;
using Qenex.QSuite.Protocol;
using Qenex.QSuite.Specification;

namespace Qenex.QSuite.Protocols.XcpProtocol;

public class Xcp : ProtocolBase
{
    public Xcp()
    {
        Specification = new SpecificationBase()
        {
            Name = "XCP Protocol",
            Description = "XCP Protocol",
            CreatedOn = new DateTime(2021, 11, 23),
            Version = Assembly.GetExecutingAssembly().GetName().Version ?? throw new Exception("Version not found"),
            Author = "Qenex",
            Company = "QENEX Ltd."
        };
    }
}
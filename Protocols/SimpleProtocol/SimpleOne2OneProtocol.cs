using System.Reflection;
using Qenex.QSuite.Protocol;
using Qenex.QSuite.Specification;

namespace SimpleProtocol;

public class SimpleOne2OneProtocol : ProtocolBase
{
    public SimpleOne2OneProtocol()
    {
        Specification = new SpecificationBase()
        {
            Gid = new Guid("dee33606-0965-48a1-a829-020d99b8dd44"),
            Name = "Simple One to One Protocol",
            Description = "No conversion of data is done. Data is sent as is.",
            CreatedOn = new DateTime(2021, 11, 23),
            Version = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 1),
            Author = "Qenex",
            Company = "QENEX Ltd."
        };
    }
}
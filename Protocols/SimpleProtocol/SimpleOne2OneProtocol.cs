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
            Name = "Simple One to One Protocol",
            Description = "No conversion of data is done. Data is sent as is.",
            CreatedOn = new DateTime(2021, 11, 23),
            Version = Assembly.GetExecutingAssembly().GetName().Version ?? throw new Exception("Version not found"),
            Author = "Qenex",
            Company = "QENEX Ltd."
        };
    }
}
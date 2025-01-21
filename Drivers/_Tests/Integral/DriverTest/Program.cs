using NetMQ;
using NetMQ.Sockets;

namespace DriverTest;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("NetMQ driver test");
        var request = new RequestSocket();
        var poller = new NetMQPoller();
        
    }
}
using System.Threading;
using System.Net.Sockets;

namespace CSaNLab4
{
    class Program
    {
        static void Main(string[] args)
        {
            Proxy proxyServer = new Proxy("127.0.0.1", 8080);
            proxyServer.Start();
            while (true)
            {
                Socket socket = proxyServer.Accept();
                Thread thread = new Thread(() => proxyServer.ReceiveData(socket));
                thread.Start();
            }
        }
    }
}
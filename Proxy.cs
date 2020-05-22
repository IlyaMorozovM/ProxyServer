using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;

namespace CSaNLab4
{
    public class Proxy
    {
        private int port;
        private string host;
        private TcpListener listener;
        private byte[] buffer;

        public Proxy(string host, int port)
        {
            this.host = host;
            this.port = port;
            this.listener = new TcpListener(IPAddress.Parse(this.host), this.port);
        }

        public void Start()
        {
            listener.Start();
        }

        public Socket Accept()
        {
            return listener.AcceptSocket();
        }

        public void ReceiveData(Socket client)
        {
            NetworkStream browser = new NetworkStream(client);
            buffer = new byte[20 * 1024];
            //Read data from browser
            while (true)                                                        
            {
                if (!browser.CanRead)
                    return;
                try
                {
                    browser.Read(buffer, 0, buffer.Length);
                }
                catch (IOException e) { return; }
                GetResponse(browser, buffer);
                client.Dispose();
            }
        }


        public void GetResponse(NetworkStream browser, byte[] buffer)
        {
            TcpClient server;
            string responseInfo;
            string responseCode;
            try
            {
                buffer = AbsoluteToRelative(buffer);

                string[] tempArr = Encoding.ASCII.GetString(buffer).Trim().Split(new char[] { '\r', '\n' });
                string request = tempArr.FirstOrDefault(x => x.Contains("Host"));
                request = request.Substring(request.IndexOf(":") + 2);

                //Get name and port (if possible)
                string[] hostAndPort = request.Trim().Split(new char[] { ':' });                                   

                //If we have port - coonect with this port, if no - 80
                if (hostAndPort.Length == 2)                                                                    
                {                                                                                               
                    server = new TcpClient(hostAndPort[0], int.Parse(hostAndPort[1]));
                }
                else
                {
                    server = new TcpClient(hostAndPort[0], 80);
                }

                NetworkStream serverStream = server.GetStream();                                            

                //Send data from browser to server
                serverStream.Write(buffer, 0, buffer.Length);

                //Response from server
                var bufResponse = new byte[32];      
                serverStream.Read(bufResponse, 0, bufResponse.Length);   
                
                //Send this response to browser
                browser.Write(bufResponse, 0, bufResponse.Length);

                //Get the response code
                string[] partsOfResponse = Encoding.UTF8.GetString(bufResponse).Split(new char[] { '\r', '\n' });     

                responseCode = partsOfResponse[0].Substring(partsOfResponse[0].IndexOf(" ") + 1);
                responseInfo = request + " " + responseCode;
                Console.WriteLine(responseInfo);
                serverStream.CopyTo(browser);                                                                
            }
            catch { return; }
        }

        private byte[] AbsoluteToRelative(byte[] buf)
        {
            string buffer = Encoding.ASCII.GetString(buf);
            Regex regex = new Regex(@"http:\/\/[a-z0-9а-яё\:\.]*");
            MatchCollection matches = regex.Matches(buffer);
            string host = matches[0].Value;
            buffer = buffer.Replace(host, "");
            buf = Encoding.ASCII.GetBytes(buffer);
            return buf;
        }

    }
}

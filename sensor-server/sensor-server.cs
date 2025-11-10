using System.Net;
using System.Net.Sockets;
using System.Text;
using NetCoreServer;

namespace sensorserver
{
    class UdpSensorServer : UdpServer
    {
        public UdpSensorServer(IPAddress address, int port) : base(address, port) { }

        protected override void OnStarted()
        {
            // Start receive datagrams
            ReceiveAsync();
        }

        protected override void OnReceived(EndPoint endpoint, byte[] buffer, long offset, long size)
        {
            Console.WriteLine("Incoming: " + Encoding.UTF8.GetString(buffer, (int)offset, (int)size));

            // Echo the message back to the sender
            SendAsync(endpoint, buffer, 0, size);
        }

        protected override void OnSent(EndPoint endpoint, long sent)
        {
            // Continue receive datagrams
            ReceiveAsync();
        }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"Echo UDP server caught an error with code {error}");
        }
    }

    class TcpConnection : TcpSession
    {
        public TcpConnection(TcpServer server) : base(server) { }

        protected override void OnConnected()
        {
            Console.WriteLine($"Chat TCP session with Id {Id} connected!");

            // Send invite message
            string message = "Hello from TCP chat! Please send a message or '!' to disconnect the client!";
            SendAsync(message);
        }

        protected override void OnDisconnected()
        {
            Console.WriteLine($"Chat TCP session with Id {Id} disconnected!");
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            string message = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);
            Console.WriteLine("Incoming: " + message);

            // Multicast message to all connected sessions
            Server.Multicast(message);

            // If the buffer starts with '!' the disconnect the current session
            if (message == "!")
                Disconnect();
        }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"Chat TCP session caught an error with code {error}");
        }
    }

    class TcpSensorServer : TcpServer
    {
        public TcpSensorServer(IPAddress address, int port) : base(address, port) { }

        protected override TcpSession CreateSession() { return new TcpConnection(this); }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"Chat TCP server caught an error with code {error}");
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            int udpPort = 3333;
            int tcpPort = 4444;

            Console.WriteLine($"UDP server port: {udpPort}");
            Console.WriteLine($"TCP server port: {tcpPort}");
            Console.WriteLine();

            // Create a new UDP server
            var udpServer = new UdpSensorServer(IPAddress.Any, port);
            var tcpServer = new TcpSensorServer(IPAddress.Any, tcpPort);

            // Start the servers
            Console.Write("Server starting...");
            udpServer.Start();
            tcpServer.Start();
            Console.WriteLine("Done!");

            Console.WriteLine("Press Enter to stop the server or '!' to restart the server...");
            for (; ; )
            {
                string line = Console.ReadLine();
                if (string.IsNullOrEmpty(line))
                    break;

                // Restart the udpServer
                if (line == "!")
                {
                    Console.Write("Server restarting...");
                    udpServer.Restart();
                    tcpServer.Restart();
                    Console.WriteLine("Done!");
                }
            }

            // Stop the servers
            Console.Write("Server stopping...");
            udpServer.Stop();
            tcpServer.Stop();
            Console.WriteLine("Done!");
        }
    }
}

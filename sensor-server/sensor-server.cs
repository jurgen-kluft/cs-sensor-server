using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace sensorserver
{
     class UdsConnection : UdsSession
    {
        public UdsConnection(UdsServer server) : base(server) {}

        protected override void OnConnected()
        {
            Console.WriteLine($"Unix Domain Socket connection with Id {Id} connected!");

            // Send invite message
            string message = "Hello from Unix Domain Socket chat! Please send a message or '!' to disconnect the client!";
            SendAsync(message);
        }

        protected override void OnDisconnected()
        {
            Console.WriteLine($"Unix Domain Socket connection with Id {Id} disconnected!");
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
            Console.WriteLine($"Unix Domain Socket session caught an error with code {error}");
        }
    }

    class UdsSensorServer : UdsServer
    {
        public UdsSensorServer(string path) : base(path) { }

        protected override UdsSession CreateSession() { return new UdsConnection(this); }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"Unix Domain Socket server caught an error with code {error}");
        }
    }

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
            Console.WriteLine($"UDP server caught an error with code {error}");
        }
    }

    class TcpConnection : TcpSession
    {
        public TcpConnection(TcpServer server) : base(server) { }

        protected override void OnConnected()
        {
            Console.WriteLine($"TCP connection with Id {Id} connected!");

            // Send invite message
            string message = "Hello from TCP ! Please send a message or '!' to disconnect the client!";
            SendAsync(message);
        }

        protected override void OnDisconnected()
        {
            Console.WriteLine($"TCP connection with Id {Id} disconnected!");
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
            Console.WriteLine($"TCP session caught an error with code {error}");
        }
    }

    class TcpSensorServer : TcpServer
    {
        public TcpSensorServer(IPAddress address, int port) : base(address, port) { }

        protected override TcpSession CreateSession() { return new TcpConnection(this); }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"TCP server caught an error with code {error}");
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            int udpPort = 31370;
            int tcpPort = 31372;
            string udsPort = "/tmp/sensor_server.sock";

            Console.WriteLine($"Servers: udp:{udpPort} tcp:{tcpPort}, uds:{udsPort}");
            Console.WriteLine();

            // Creates new UDP/TCP and UDS servers
            var udpServer = new UdpSensorServer(IPAddress.Any, udpPort);
            var tcpServer = new TcpSensorServer(IPAddress.Any, tcpPort);
            var udsServer = new UdsSensorServer(udsPort);

            // Start the servers
            Console.Write("Servers starting...");
            udpServer.Start();
            tcpServer.Start();
            udsServer.Start();
            Console.WriteLine("Done!");

            Console.WriteLine("Press Enter to stop the servers or '!' to restart the servers...");
            for (; ; )
            {
                string line = Console.ReadLine();
                if (string.IsNullOrEmpty(line))
                    break;

                // Restart the udpServer
                if (line == "!")
                {
                    Console.Write("Servers restarting...");
                    udpServer.Restart();
                    tcpServer.Restart();
                    udsServer.Restart();
                    Console.WriteLine("Done!");
                }
            }

            // Stop the servers
            Console.Write("Servers stopping...");
            udpServer.Stop();
            tcpServer.Stop();
            udsServer.Stop();
            Console.WriteLine("Done!");
        }
    }
}

using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;


namespace Server
{
    public class Program
    {
        private static int port = 8080;
        private static ConcurrentDictionary<string, Game> games = new ConcurrentDictionary<string, Game>();
        private static ConcurrentDictionary<string, string> players = new ConcurrentDictionary<string, string>();
        public static Queue<string> WaitingPlayers { get; } = new Queue<string>();

        static void StartServer()
        {
            // Set up the server socket.
            IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
            // IPAddress ipAddress = IPAddress.Any;
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, port);
            Socket server = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                server.Bind(localEndPoint);
                server.Listen(10);

                Console.WriteLine("Listening at " + ipAddress + ":" + port);

                while (true)
                {
                    Socket client = server.Accept();

                    // Handle the client connection in a separate thread
                    HandleClient handleClient = new HandleClient(client, games, players, WaitingPlayers);
                    Thread clientThread = new Thread(() => handleClient.HandleClientRequest(client));
                    clientThread.Start();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        static void Main(string[] args)
        {
            StartServer();
        }
    }
}


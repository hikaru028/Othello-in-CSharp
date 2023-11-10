using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Security.Cryptography;
using System.Threading;
using System.Text;
using System.Web;
using System.Text.Json;

namespace Server
{
    public class HandleClient
    {
        private Socket client;
        private ConcurrentDictionary<string, Game> games;
        private ConcurrentDictionary<string, string> players;
        private Queue<string> waitingPlayers;

        public HandleClient(Socket client, ConcurrentDictionary<string, Game> games, ConcurrentDictionary<string, string> players, Queue<string> waitingPlayers)
        {
            this.client = client;
            this.games = games;
            this.players = players;
            this.waitingPlayers = waitingPlayers;
        }

        public void HandleClientRequest(Socket client)
        {
            string clientAddress = client.RemoteEndPoint?.ToString() ?? "Unknown";
            Console.WriteLine("Connection established with " + clientAddress);

            try
            {
                while (true)
                {
                    // To Read the request from the client
                    byte[] buffer = new byte[1024];
                    int bytesRead = client.Receive(buffer);
                    string request = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                    string[] requestLines = request.Split('\n');
                    if (requestLines.Length < 1)
                    {
                        return;
                    }

                    string requestLine = requestLines[0].Trim();
                    string[] requestParts = requestLine.Split(' ');

                    if (requestParts.Length < 2)
                    {
                        return;
                    }

                    string method = requestParts[0];
                    string path = requestParts[1];

                    // To Send a response to the client
                    string response = ProcessRequest(path);
                    byte[] responseBytes = Encoding.ASCII.GetBytes(response);
                    client.Send(responseBytes);
                    Console.WriteLine("Thread " + Thread.CurrentThread.ManagedThreadId + " sent a response to " + clientAddress + " for " + path);
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine("IO Exception >>> " + ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error >>> " + ex.ToString());
            }

            client.Close();
            Console.WriteLine("Thread " + Thread.CurrentThread.ManagedThreadId + " closing the connection with " + clientAddress + " and terminating");

        }


        public string ProcessRequest(string path)
        {
            string endpoint = path;
            string response = "";
            string faviconHeaders = "HTTP/1.1 200 OK\r\n" +
                "Content-Type: image/x-icon\r\n" +
                "Access-Control-Allow-Origin: *\r\n" +
                "Access-Control-Allow-Methods: GET,POST\r\n" +
                "Access-Control-Allow-Headers: Content-Type\r\n\r\n";
            string headers = "HTTP/1.1 200 OK\r\n" +
                "Content-Type: text/plain\r\n" +
                "Access-Control-Allow-Origin: *\r\n" +
                "Access-Control-Allow-Methods: GET,POST\r\n" +
                "Access-Control-Allow-Headers: Content-Type\r\n\r\n";

            if (endpoint == "/favicon.ico")
            {
                string currentDirectory = Directory.GetCurrentDirectory();
                string faviconPath = Path.Combine(currentDirectory, "..", "..", "..", "root", "favicon.ico");
                byte[] faviconData = File.ReadAllBytes(faviconPath);
                response = faviconHeaders;

                byte[] responseBytes = Encoding.ASCII.GetBytes(response);
                Console.WriteLine(responseBytes);
                Console.WriteLine(faviconData);
                client.Send(responseBytes);
                client.Send(faviconData);
            }
            else if (endpoint == "/register")
            {
                string username = GenerateUsername();
                response = headers + username;
            }
            else if (endpoint.StartsWith("/pairme"))
            {
                string gameRecord = PairPlayer(endpoint);
                response = headers + gameRecord;
            }
            else if (endpoint.StartsWith("/mymove"))
            {
                string currentProcess = UpdateMyMove(endpoint);
                response = headers + JsonSerializer.Serialize(new { currentProcess });

            }
            else if (endpoint.StartsWith("/theirmove"))
            {
                string opponentMove = GetOpponentMove(endpoint);
                response = headers + JsonSerializer.Serialize(new { opponentMove });
            }
            else if (endpoint.StartsWith("/quit"))
            {
                string result = QuitGame(endpoint);
                response = result;
            }
            else
            {
                response = "Invalid endpoint";
            }

            return response;

        }


        // To generate a username when entering "/register" page
        public string GenerateUsername()
        {
            Random random = new Random();
            string username;

            lock (players)
            {
                do
                {
                    username = "Player" + random.Next(1000, 9999);
                }
                while (players.ContainsKey(username));

                players.TryAdd(username, username);
            }

            return username;
        }


        // To find an opponent player
        public string PairPlayer(string endpoint)
        {
            string username = GetQueryParamValue(endpoint, "player");
            if (string.IsNullOrEmpty(username) || !players.ContainsKey(username))
            {
                return "Invalid player";
            }

            Game game;
            lock (games)
            {
                if (games.ContainsKey(username))
                {
                    // The player has already been paired, so retrieve their game
                    game = games[username];
                }
                else if (waitingPlayers.Count > 0)
                {
                    // There's a waiting player, then pair the current player with the waiting player
                    string waitingPlayer = waitingPlayers.Dequeue();
                    game = new Game(waitingPlayer, username);
                    game.State = "progress";

                    // Make the game accessible by both players' usernames
                    games[waitingPlayer] = game;
                    games[username] = game;
                }
                else
                {
                    // There's no waiting player, so create a new game for the requesting player and add them to the queue
                    game = new Game(username);
                    games[username] = game;
                    waitingPlayers.Enqueue(username);
                }
            }
            return game.GetGameRecord(game);
        }


        // To update own movement 
        public string UpdateMyMove(string endpoint)
        {
            string player = GetQueryParamValue(endpoint, "player");
            string gameId = GetQueryParamValue(endpoint, "id");
            string move = GetQueryParamValue(endpoint, "move");

            if (!games.ContainsKey(player))
            {
                return "Player is not part of the game";
            }

            if (games[player].GameId != gameId)
            {
                return "Game not found";
            }

            Game game = games[player];
            if (game.State != "progress")
            {
                return "The game is not in progress";
            }

            if (player == game.Player1)
            {
                game.LastMovePlayer1 = move;

            }
            else if (player == game.Player2)
            {
                game.LastMovePlayer2 = move;

            }

            return "Your move was sent";
        }


        // To retrieve the opponent player's movement
        public string GetOpponentMove(string endpoint)
        {
            string player = GetQueryParamValue(endpoint, "player");
            string gameId = GetQueryParamValue(endpoint, "id");

            if (!games.ContainsKey(player))
            {
                return "Player does not exist";
            }

            Game game = games[player];
            if (game.GameId != gameId)
            {
                return "Game ID does not match";
            }

            if (game.State != "progress")
            {
                return "The game is not in progress";
            }

            if (player == game.Player1 && !string.IsNullOrEmpty(game.LastMovePlayer2))
            {
                return game.LastMovePlayer2;
            }
            else if (player == game.Player2 && !string.IsNullOrEmpty(game.LastMovePlayer1))
            {
                return game.LastMovePlayer1;
            }

            return "Player not recognised in the game";
        }


        // To quit the current game
        public string QuitGame(string endpoint)
        {
            string player = GetQueryParamValue(endpoint, "player");
            string gameId = GetQueryParamValue(endpoint, "id");

            if (!games.ContainsKey(player))
            {
                return "Player does not exist";
            }

            Game game = games[player];
            if (game.GameId != gameId)
            {
                return "Game ID does not match";
            }

            if (game.Player1 != null)
            {
                players.TryRemove(game.Player1, out _);
            }

            if (game.Player2 != null)
            {
                players.TryRemove(game.Player2, out _);
            }

            return "Game quit successfully";
        }


        // To obtain each prameter's value
        public string GetQueryParamValue(string endpoint, string parameterName)
        {
            Uri uri = new Uri("http://127.0.0.1" + endpoint);
            string value = HttpUtility.ParseQueryString(uri.Query).Get(parameterName) ?? string.Empty;
            if (!string.IsNullOrEmpty(value) && !players.ContainsKey(value))
            {
                players.TryAdd(value, value);
            }
            return value;
        }
    }
}

# Two-player Othello Game in C#

## Overview
This project is a multithreaded game server for a two-player Othello game. The server pairs up players and coordinates the exchange of game moves. It is built using the C# programming language and .NET 7, employing synchronous server sockets for communication. The server interacts with game clients through HTTP REST endpoints.

## Features
Player Registration: Players can register to get a unique username.
Player Pairing: The server pairs players for a game session.
Move Coordination: The server manages the exchange of moves between players.
Game Quit Handling: Players can gracefully exit a game session.

## Endpoints
`/register`
- Generates a random username for a player, registers it, and returns the username.

`/pairme?player={username}`
- Attempts to pair the given player with another player. Returns a game record with relevant details. If no other player is waiting, it returns a game record indicating the "wait" state.

`/mymove?player={username}&id={gameId}&move={move}`
- Supplies the player's move to the server during the game's "progress" state. Updates the game record with the supplied move.

`/theirmove?player={username}&id={gameId}`
Collects the other player's move from the server during the game's "progress" state. Returns the last move of the other player from the game record.

`/quit?player={username}&id={gameId}`
Informs the server that the player wants to quit the game. The server removes the corresponding game record.

## Getting Started
# Prerequisites
.NET 7 SDK
A text editor or IDE for C# development


# Set up
1. In the terminal, navigate to the directory containing your Program.cs file.
```
cd Server
```

2. Build the program:
```
dotnet build
```

3. Run the programme:
```
dotnet run
```

It should show like `Listening at 127.0.0.1:8080`.

4. Open client/wwwroot/`index.html` file 

## Testing Strategy
The server can be tested using Telnet to ensure it handles various scenarios correctly. Additionally, a browser-based client can be developed for a more interactive testing experience.

## Logging
The server logs client IP and port number upon connection establishment, along with the accessed URL and thread ID.

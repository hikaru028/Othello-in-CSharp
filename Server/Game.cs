public class Game
{
    public string GameId { get; set; }
    public string State { get; set; }
    public string Player1 { get; set; }
    public string? Player2 { get; set; }
    public string? LastMovePlayer1 { get; set; }
    public string? LastMovePlayer2 { get; set; }

    public Game(string player1)
    {
        GameId = Guid.NewGuid().ToString();
        State = "wait";
        Player1 = player1;
    }

    public Game(string player1, string player2)
    {
        GameId = Guid.NewGuid().ToString();
        State = "progress";
        Player1 = player1;
        Player2 = player2;
    }

    public string GetGameRecord(Game game)
    {
        var gameDetails = new
        {
            gameId = game.GameId,
            state = game.State,
            player1 = game.Player1,
            player2 = game.Player2,
            lastMovePlayer1 = game.LastMovePlayer1,
            lastMovePlayer2 = game.LastMovePlayer2,
        };

        return System.Text.Json.JsonSerializer.Serialize(gameDetails);
    }
}
namespace CheckersGame.Models;

public class GameRoomDto
{
    public string Id { get; set; } = string.Empty;
    public bool IsPrivate { get; set; }
    public string Name { get; set; } = string.Empty;
}
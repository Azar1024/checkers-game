namespace CheckersGame.Models;

public class MoveDto
{
    public int FromRow { get; set; }
    public int FromCol { get; set; }
    public int ToRow { get; set; }
    public int ToCol { get; set; }
    public bool IsCapture { get; set; }
}
namespace CheckersGame.Models;

public record Move(int FromRow, int FromCol, int ToRow, int ToCol, bool IsCapture = false);
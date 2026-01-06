using System;

namespace CheckersGame.Models;

public record Move(int FromRow, int FromCol, int ToRow, int ToCol)
{
    public bool IsCapture => Math.Abs(ToRow - FromRow) == 2;
}

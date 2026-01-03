using System.Collections.Generic;
using System.Linq;

namespace checkers_game.Models;

public class Board
{
    public const int Size = 8;
    private readonly Piece?[,] _grid = new Piece?[Size, Size];

    public Board()
    {
        // Белые внизу (rows 5-7), чёрные сверху (rows 0-2)
        for (int r = 0; r < 3; r++)
            for (int c = (r % 2); c < Size; c += 2)
                _grid[r, c] = new Piece(Player.Black, PieceType.Regular);

        for (int r = 5; r < Size; r++)
            for (int c = (r % 2); c < Size; c += 2)
                _grid[r, c] = new Piece(Player.White, PieceType.Regular);
    }

    public Piece? this[int row, int col]
    {
        get => _grid[row, col];
        private set => _grid[row, col] = value;
    }
}
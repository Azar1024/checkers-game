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

    public void Execute(Move move)
    {
        var piece = this[move.FromRow, move.FromCol];
        this[move.FromRow, move.FromCol] = null;
        this[move.ToRow, move.ToCol] = piece;

        // Удаление захваченной шашки
        if (move.IsCapture)
        {
            int dr = (move.ToRow - move.FromRow) / 2;
            int dc = (move.ToCol - move.FromCol) / 2;
            this[move.FromRow + dr, move.FromCol + dc] = null;
        }

        // Превращение в дамку
        if (piece?.Owner == Player.White && move.ToRow == 0)
            this[move.ToRow, move.ToCol] = piece with { Type = PieceType.King };
        if (piece?.Owner == Player.Black && move.ToRow == Size - 1)
            this[move.ToRow, move.ToCol] = piece with { Type = PieceType.King };
    }

    public IEnumerable<Move> GetLegalMoves(int row, int col, Player current)
    {
        var piece = this[row, col];
        if (piece?.Owner != current) yield break;

        var dirs = piece.IsKing
            ? new[] { (-1, -1), (-1, 1), (1, -1), (1, 1) }
            : piece.IsWhite
                ? new[] { (-1, -1), (-1, 1) }
                : new[] { (1, -1), (1, 1) };

        foreach (var (dr, dc) in dirs)
        {
            // Обычный ход
            int nr = row + dr, nc = col + dc;
            if (IsInside(nr, nc) && this[nr, nc] == null)
                yield return new Move(row, col, nr, nc);

            // Прыжок
            int jr = row + 2 * dr, jc = col + 2 * dc;
            if (IsInside(nr, nc) && IsInside(jr, jc) &&
                this[nr, nc]?.Owner == Opponent(current) &&
                this[jr, jc] == null)
                yield return new Move(row, col, jr, jc);
        }
    }

    private static Player Opponent(Player p) => p == Player.White ? Player.Black : Player.White;
    private static bool IsInside(int r, int c) => r >= 0 && r < Size && c >= 0 && c < Size;
}
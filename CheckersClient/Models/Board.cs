using System.Collections.Generic;
using System.Linq;
using System;

namespace CheckersGame.Models;

public class Board
{
    public const int Size = 8;

    private readonly Piece?[,] _grid = new Piece?[Size, Size];

    public Board()
    {
        //  ИНИЦИАЛИЗАЦИЯ СТАРТОВОЙ ПОЗИЦИИ 
        // Черные шашки в верхних трех рядах (индексы 0-2)
        for (int r = 0; r < 3; r++)
            for (int c = (r % 2); c < Size; c += 2) // Чередуем клетки для темного фона
                _grid[r, c] = new Piece(Player.Black, PieceType.Regular);

        // Белые шашки в нижних трех рядах (индексы 5-7)
        for (int r = 5; r < Size; r++)
            for (int c = (r % 2); c < Size; c += 2)
                _grid[r, c] = new Piece(Player.White, PieceType.Regular);
    }

    public Piece? this[int row, int col]
    {
        get => _grid[row, col];
        private set => _grid[row, col] = value;
    }

    //  ОСНОВНОЙ МЕТОД ВЫПОЛНЕНИЯ ХОДА 
    public void Execute(Move move)
    {
        var piece = this[move.FromRow, move.FromCol];
        if (piece == null) return;

        //  ПЕРЕМЕЩЕНИЕ ШАШКИ 
        this[move.FromRow, move.FromCol] = null;
        this[move.ToRow, move.ToCol] = piece;

        //  ОБРАБОТКА ВЗЯТИЯ 
        if (move.IsCapture)
        {
            if (!piece.IsKing)
            {
                //  ВЗЯТИЕ ОБЫЧНОЙ ШАШКОЙ - ПО ПРЯМОЙ 
                int capturedRow = (move.FromRow + move.ToRow) / 2;
                int capturedCol = (move.FromCol + move.ToCol) / 2;
                this[capturedRow, capturedCol] = null;
            }
            else
            {
                //  ВЗЯТИЕ ДАМКОЙ - ПО ДИАГОНАЛИ ДО ПЕРВОГО ВРАГА 
                int dr = move.ToRow > move.FromRow ? 1 : -1;
                int dc = move.ToCol > move.FromCol ? 1 : -1;
                int r = move.FromRow + dr;
                int c = move.FromCol + dc;

                while (r != move.ToRow && c != move.ToCol)
                {
                    if (this[r, c] != null && this[r, c].Owner != piece.Owner)
                    {
                        this[r, c] = null; // Удаляем первую встреченную вражескую шашку
                        break;
                    }
                    r += dr;
                    c += dc;
                }
            }
        }

        //  ЛОГИКА ПРЕВРАЩЕНИЯ В ДАМКУ 
        // В русских шашках, если во время боя шашка касается дамочного поля,
        // она становится дамкой сразу же.
        bool promoted = false;
        if (piece.IsWhite && move.ToRow == 0 && !piece.IsKing)
        {
            // Белая шашка достигла верхнего ряда (индекс 0)
            this[move.ToRow, move.ToCol] = piece with { Type = PieceType.King };
            promoted = true;
        }
        else if (!piece.IsWhite && move.ToRow == Size - 1 && !piece.IsKing)
        {
            // Черная шашка достигла нижнего ряда (индекс 7)
            this[move.ToRow, move.ToCol] = piece with { Type = PieceType.King };
            promoted = true;
        }

        // Обновляем ссылку на piece, если произошло превращение (для дальнейшей логики, если понадобится)
        if (promoted) piece = this[move.ToRow, move.ToCol];
    }

    // ГЕНЕРАТОР ВСЕХ ЛЕГАЛЬНЫХ ХОДОВ ДЛЯ КОНКРЕТНОЙ ШАШКИ
    public IEnumerable<Move> GetLegalMoves(int row, int col, Player current)
    {
        var piece = this[row, col];
        if (piece?.Owner != current) yield break;

        if (piece.IsKing)
        {
            //  ХОДЫ ДАМКИ - ВО ВСЕХ ЧЕТЫРЕХ НАПРАВЛЕНИЯХ 
            var directions = new[] { (-1, -1), (-1, 1), (1, -1), (1, 1) };
            foreach (var (dr, dc) in directions)
            {
                foreach (var move in GetSimpleMovesForKing(row, col, dr, dc, current))
                    yield return move;
                foreach (var move in GetCaptureMovesForKing(row, col, dr, dc, current))
                    yield return move;
            }
        }
        else
        {
            //  ОБЫЧНАЯ ШАШКА 
            // 1. Простые ходы (Только вперед)
            var moveDirections = piece.IsWhite
                ? new[] { (-1, -1), (-1, 1) } // Белые вверх
                : new[] { (1, -1), (1, 1) }; // Черные вниз
            foreach (var (dr, dc) in moveDirections)
            {
                int nextRow = row + dr;
                int nextCol = col + dc;
                if (IsInside(nextRow, nextCol) && this[nextRow, nextCol] == null)
                {
                    yield return new Move(row, col, nextRow, nextCol, IsCapture: false);
                }
            }

            // 2. Взятия (Вперед и НАЗАД - правило русских шашек)
            // Важная особенность: обычные шашки могут бить назад!
            var captureDirections = new[] { (-1, -1), (-1, 1), (1, -1), (1, 1) };
            foreach (var (dr, dc) in captureDirections)
            {
                int jumpRow = row + 2 * dr;
                int jumpCol = col + 2 * dc;
                int capturedRow = row + dr;
                int capturedCol = col + dc;

                if (IsInside(jumpRow, jumpCol) &&
                    IsInside(capturedRow, capturedCol) &&
                    this[capturedRow, capturedCol]?.Owner == Opponent(current) &&
                    this[jumpRow, jumpCol] == null)
                {
                    yield return new Move(row, col, jumpRow, jumpCol, IsCapture: true);
                }
            }
        }
    }

    // ПОМОЩНИК: ПРОСТЫЕ ХОДЫ ДАМКИ ПО ДИАГОНАЛИ
    private IEnumerable<Move> GetSimpleMovesForKing(int row, int col, int dr, int dc, Player current)
    {
        int r = row + dr;
        int c = col + dc;
        while (IsInside(r, c) && this[r, c] == null)
        {
            yield return new Move(row, col, r, c, IsCapture: false);
            r += dr;
            c += dc;
        }
    }

    // ПОМОЩНИК: ВЗЯТИЯ ДАМКИ ПО ДИАГОНАЛИ 
    private IEnumerable<Move> GetCaptureMovesForKing(int row, int col, int dr, int dc, Player current)
    {
        int r = row + dr;
        int c = col + dc;

        // Идем по диагонали до первого препятствия
        while (IsInside(r, c) && this[r, c] == null)
        {
            r += dr;
            c += dc;
        }

        // Если встретили врага, проверяем возможность прыжка
        if (IsInside(r, c) && this[r, c]?.Owner == Opponent(current))
        {
            int jumpR = r + dr;
            int jumpC = c + dc;
            while (IsInside(jumpR, jumpC) && this[jumpR, jumpC] == null)
            {
                yield return new Move(row, col, jumpR, jumpC, IsCapture: true);
                jumpR += dr;
                jumpC += dc;
            }
        }
    }

    // СОБИРАЕТ ВСЕ ЛЕГАЛЬНЫЕ ХОДЫ ДЛЯ ВСЕХ ШАШЕК ИГРОКА
    public IEnumerable<Move> GetAllLegalMoves(Player current)
    {
        var allMoves = new List<Move>();
        for (int r = 0; r < Size; r++)
        {
            for (int c = 0; c < Size; c++)
            {
                if (this[r, c]?.Owner == current)
                {
                    allMoves.AddRange(GetLegalMoves(r, c, current));
                }
            }
        }
        return allMoves;
    }

    private static Player Opponent(Player p) => p == Player.White ? Player.Black : Player.White;
    private static bool IsInside(int r, int c) => r >= 0 && r < Size && c >= 0 && c < Size;
}
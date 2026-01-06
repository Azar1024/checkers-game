namespace CheckersGame.Models;

public enum Player { White, Black }
public enum PieceType { Regular, King }

public record Piece(Player Owner, PieceType Type)
{
    public bool IsWhite => Owner == Player.White;
    public bool IsKing => Type == PieceType.King;
}

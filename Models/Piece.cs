namespace CheckersGame.Models;

//  ÎÑÍÎÂÍÛÅ ÒÈÏÛ ØÀØÅÊ 
public enum Player { White, Black }           // Âëàäåëåö øàøêè
public enum PieceType { Regular, King }       // Òèï øàøêè

//  ÍÅÈÇÌÅÍßÅÌÀß ÑÒÐÓÊÒÓÐÀ ØÀØÊÈ 
public record Piece(Player Owner, PieceType Type)
{
    public bool IsWhite => Owner == Player.White;
    public bool IsKing => Type == PieceType.King;
}
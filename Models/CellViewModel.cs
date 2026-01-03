using CommunityToolkit.Mvvm.ComponentModel;
using checkers_game.Models;

namespace checkers_game.Models;

public class CellViewModel : ObservableObject
{
    private Piece? _piece;
    public Piece? Piece
    {
        get => _piece;
        set
        {
            if (SetProperty(ref _piece, value))
            {
                OnPropertyChanged(nameof(IsOccupied));
                OnPropertyChanged(nameof(IsWhite));
                OnPropertyChanged(nameof(IsKing));
                OnPropertyChanged(nameof(IsBlack));
            }
        }
    }

    public int Row { get; set; }
    public int Col { get; set; }

    public bool IsOccupied => Piece != null;
    public bool IsWhite => Piece?.IsWhite == true;
    public bool IsKing => Piece?.IsKing == true;
    public bool IsBlack => Piece != null && Piece.IsWhite == false;

    public CellViewModel(Piece? piece, int row, int col)
    {
        _piece = piece;
        Row = row;
        Col = col;
    }
}
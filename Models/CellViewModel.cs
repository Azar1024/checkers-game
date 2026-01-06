using CommunityToolkit.Mvvm.ComponentModel;
using CheckersGame.Models;

namespace CheckersGame.Models;

public class CellViewModel : ObservableObject
{
    private Piece? _piece;

    //  ОСНОВНОЕ СВОЙСТВО С MVVM-УВЕДОМЛЕНИЯМИ 
    // При изменении шашки в клетке обновляются все зависимые свойства UI
    public Piece? Piece
    {
        get => _piece;
        set
        {
            if (SetProperty(ref _piece, value))
            {
                //  ОБНОВЛЯЕМ ВСЕ СВОЙСТВА UI 
                OnPropertyChanged(nameof(IsOccupied));
                OnPropertyChanged(nameof(IsWhite));
                OnPropertyChanged(nameof(IsKing));
                OnPropertyChanged(nameof(IsBlack)); 
            }
        }
    }

    public int Row { get; set; }
    public int Col { get; set; }

    //  ВЫЧИСЛЯЕМЫЕ СВОЙСТВА ДЛЯ ПРИВЯЗКИ К UI 
    public bool IsOccupied => Piece != null;
    public bool IsWhite => Piece?.IsWhite == true;
    public bool IsKing => Piece?.IsKing == true;

    // true только если есть шашка и она чёрная
    // Предотвращает ложные срабатывания при null
    public bool IsBlack => Piece != null && Piece.IsWhite == false;

    // КОНСТРУКТОР 
    public CellViewModel(Piece? piece, int row, int col)
    {
        _piece = piece;
        Row = row;
        Col = col;
    }
}
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using checkers_game.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace checkers_game.ViewModels;

public partial class GameViewModel : ObservableObject
{
    private readonly Board _board = new();
    [ObservableProperty] private Player _currentPlayer = Player.White;
    [ObservableProperty] private (int row, int col)? _selected;
    [ObservableProperty] private string _status = "Выберите режим";
    [ObservableProperty] private GameMode _gameMode;
    [ObservableProperty] private bool _isStartScreen = true;

    private readonly IReadOnlyList<CellViewModel> _grid;

    public IReadOnlyList<CellViewModel> Grid => _grid;

    public ICommand CellCommand { get; }
    public ICommand StartHumanVsHumanCommand { get; }
    public ICommand StartHumanVsAICommand { get; }

    public GameViewModel()
    {
        // Инициализация при запуске
        _grid = CreateInitialGrid();
        CellCommand = new RelayCommand<CellViewModel>(OnCellClick);
        StartHumanVsHumanCommand = new RelayCommand(() => StartGame(GameMode.HumanVsHuman));
        StartHumanVsAICommand = new RelayCommand(() => StartGame(GameMode.HumanVsAI));
    }

    private IReadOnlyList<CellViewModel> CreateInitialGrid()
    {
        var newGrid = new List<CellViewModel>();
        for (int r = 0; r < Board.Size; r++)
        {
            for (int c = 0; c < Board.Size; c++)
            {
                newGrid.Add(new CellViewModel(_board[r, c], r, c));
            }
        }
        return newGrid.AsReadOnly();
    }

    private void StartGame(GameMode mode)
    {
        GameMode = mode;
        IsStartScreen = false;
        CurrentPlayer = Player.White;
        Status = "Ход белых";
        Selected = null;
    }

    private void OnCellClick(CellViewModel? cellVm)
    {
        if (cellVm == null || IsStartScreen) return;
        int row = cellVm.Row;
        int col = cellVm.Col;

        if (Selected is { } sel)
        {
            var move = new Move(sel.row, sel.col, row, col);
            if (IsLegal(move))
            {
                _board.Execute(move);
                RefreshGrid();
                Selected = null;
                SwitchPlayer();
                CheckGameEnd();
                return;
            }
        }

        var piece = _board[row, col];
        if (piece?.Owner == CurrentPlayer)
            Selected = (row, col);
        else
            Selected = null;
    }

    private bool IsLegal(Move m)
    {
        var legal = _board.GetLegalMoves(m.FromRow, m.FromCol, CurrentPlayer);
        return legal.Any(l => l.Equals(m));
    }

    private void SwitchPlayer()
    {
        CurrentPlayer = CurrentPlayer == Player.White ? Player.Black : Player.White;
        Status = $"Ход {(CurrentPlayer == Player.White ? "белых" : "чёрных")}";
    }

    private void RefreshGrid()
    {
        for (int r = 0; r < Board.Size; r++)
            for (int c = 0; c < Board.Size; c++)
                _grid[r * Board.Size + c].Piece = _board[r, c];
    }

    private void CheckGameEnd()
    {
        // Заглушка
    }
}
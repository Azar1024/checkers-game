using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CheckersGame.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CheckersGame.ViewModels;

public partial class GameViewModel : ObservableObject
{
    // УБРАН readonly — нужно пересоздавать доску при новой игре
    private Board _board;

    [ObservableProperty] private Player _currentPlayer = Player.White;
    [ObservableProperty] private (int row, int col)? _selected;
    [ObservableProperty] private string _status = "Выберите режим";
    [ObservableProperty] private GameMode _gameMode;
    [ObservableProperty] private bool _isStartScreen = true;
    [ObservableProperty] private bool _isAnimatingStart = false;

    // Grid — mutable поле
    private IReadOnlyList<CellViewModel> _grid;
    public IReadOnlyList<CellViewModel> Grid => _grid;

    public ICommand CellCommand { get; }
    public ICommand StartHumanVsHumanCommand { get; }
    public ICommand StartHumanVsAICommand { get; }

    public GameViewModel()
    {
        // Инициализация при запуске
        _board = new Board();
        CreateGrid();

        CellCommand = new RelayCommand<CellViewModel>(OnCellClick);
        StartHumanVsHumanCommand = new RelayCommand(() => StartGame(GameMode.HumanVsHuman));
        StartHumanVsAICommand = new RelayCommand(() => StartGame(GameMode.HumanVsAI));
    }

    private async void StartGame(GameMode mode)
    {
        GameMode = mode;
        _isAnimatingStart = true;
        await Task.Delay(500);
        _isAnimatingStart = false;
        IsStartScreen = false;

        // Пересоздаём доску и Grid
        _board = new Board();  // Теперь можно — не readonly
        CreateGrid();

        CurrentPlayer = Player.White;
        Status = "Ход белых";
        Selected = null;
    }

    private void CreateGrid()
    {
        var newGrid = Enumerable.Range(0, Board.Size)
            .SelectMany(r => Enumerable.Range(0, Board.Size)
                .Select(c => new CellViewModel(_board[r, c], r, c)))
            .ToArray();

        _grid = newGrid;
        OnPropertyChanged(nameof(Grid)); // Уведомляем UI
    }

    private async void OnCellClick(CellViewModel? cellVm)
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

                if (!IsStartScreen && GameMode == GameMode.HumanVsAI && CurrentPlayer == Player.Black)
                {
                    await Task.Delay(300);
                    MakeAIMove();
                }
                return;
            }
        }

        var piece = _board[row, col];
        if (piece?.Owner == CurrentPlayer)
            Selected = (row, col);
        else
            Selected = null;
    }

    private void MakeAIMove()
    {
        var bestMove = GetBestMove(Player.Black, depth: 2);
        if (bestMove != null)
        {
            _board.Execute(bestMove);
            RefreshGrid();
            SwitchPlayer();
            CheckGameEnd();
        }
    }

    private Move? GetBestMove(Player aiPlayer, int depth)
    {
        var moves = GetAllLegalMoves(aiPlayer).ToList();
        if (!moves.Any()) return null;

        var random = new Random();
        var captureMoves = moves.Where(m => m.IsCapture).ToList();
        var candidates = captureMoves.Any() ? captureMoves : moves;
        return candidates[random.Next(candidates.Count)];
    }

    private IEnumerable<Move> GetAllLegalMoves(Player player)
    {
        for (int r = 0; r < Board.Size; r++)
            for (int c = 0; c < Board.Size; c++)
                if (_board[r, c]?.Owner == player)
                    foreach (var move in _board.GetLegalMoves(r, c, player))
                        yield return move;
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
        bool whiteHasMoves = HasAnyMove(Player.White);
        bool blackHasMoves = HasAnyMove(Player.Black);

        if (!whiteHasMoves && !blackHasMoves) Status = "Ничья!";
        else if (CurrentPlayer == Player.White && !whiteHasMoves) Status = "Победа чёрных!";
        else if (CurrentPlayer == Player.Black && !blackHasMoves) Status = "Победа белых!";
    }

    private bool HasAnyMove(Player p)
    {
        return GetAllLegalMoves(p).Any();
    }
}
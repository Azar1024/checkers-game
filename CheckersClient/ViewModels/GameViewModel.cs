using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CheckersGame.Models;
using CheckersGame.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Threading;
using Avalonia.Controls.ApplicationLifetimes;

namespace CheckersGame.ViewModels;

public partial class GameViewModel : ObservableObject
{
    //  ОСНОВНЫЕ МОДЕЛИ ДАННЫХ 
    private Board _board;
    private OnlineGameClient? _onlineClient;
    private bool _isOnlineGameActive = false; // Блокировка до начала матча

    [ObservableProperty]
    private Player _currentPlayer = Player.White;

    [ObservableProperty]
    private (int row, int col)? _selected; // Выбранная игроком шашка

    [ObservableProperty]
    private string _status = "Выберите режим";

    [ObservableProperty]
    private GameMode _gameMode;

    [ObservableProperty]
    private bool _isStartScreen = true;

    //  СОСТОЯНИЕ КОНЦА ИГРЫ 
    private bool _isGameOver;
    public bool IsGameOver
    {
        get => _isGameOver;
        set => SetProperty(ref _isGameOver, value);
    }

    // Ключевая логика русских шашек: одна шашка может делать множественные взятия за ход
    private (int row, int col)? _chainPiece = null;
    // 

    //  СЕТЬ КЛЕТОК ДЛЯ UI 
    private IReadOnlyList<CellViewModel> _grid;
    public IReadOnlyList<CellViewModel> Grid => _grid;

    //  КОМАНДЫ ДЛЯ UI 
    public ICommand CellCommand { get; }
    public ICommand StartHumanVsHumanCommand { get; }
    public ICommand StartHumanVsAICommand { get; }
    public ICommand StartOnlineGameCommand { get; }
    public ICommand ReturnToMenuCommand { get; }
    public ICommand ExitCommand { get; }

    //  ИНИЦИАЛИЗАЦИЯ VIEWMODEL 
    public GameViewModel()
    {
        _board = new Board();
        CreateGrid(); // Создаем начальную сетку клеток

        //  НАСТРОЙКА КОМАНД 
        CellCommand = new RelayCommand<CellViewModel>(OnCellClick);
        StartHumanVsHumanCommand = new RelayCommand(() => StartGame(GameMode.HumanVsHuman));
        StartHumanVsAICommand = new RelayCommand(() => StartGame(GameMode.HumanVsAI));
        StartOnlineGameCommand = new RelayCommand(StartOnlineGame);
        ReturnToMenuCommand = new RelayCommand(ReturnToMenu);
        ExitCommand = new RelayCommand(ExitApp);
    }

    //  СТАРТ НОВОЙ ИГРЫ 
    private void StartGame(GameMode mode)
    {
        GameMode = mode;
        IsStartScreen = false;
        IsGameOver = false;
        _onlineClient = null;
        
        //  ПЕРЕЗАПУСК ИГРОВОГО СОСТОЯНИЯ 
        _board = new Board();
        _chainPiece = null; // Сброс цепочки взятий
        CreateGrid();
        CurrentPlayer = Player.White;
        Status = "Ход белых";
        Selected = null;
    }

    private async void StartOnlineGame()
    {
        GameMode = GameMode.Online;
        IsStartScreen = false;
        IsGameOver = false;
        
        _board = new Board();
        _chainPiece = null;
        CreateGrid();
        CurrentPlayer = Player.White;
        Selected = null;

        Status = "Подключение к серверу...";
        _isOnlineGameActive = false; // Блокируем ходы
        _onlineClient = new OnlineGameClient();

        _onlineClient.OnWaiting += (id) =>
        {
            Dispatcher.UIThread.Invoke(() => Status = $"Ожидание соперника... (ID: {id})");
        };

        _onlineClient.OnGameStarted += () =>
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                _isOnlineGameActive = true; // Разблокируем игру
                var colorName = _onlineClient.MyColor == Player.White ? "Белыми" : "Чёрными";
                Status = $"Игра началась! Вы играете {colorName}.";
                CurrentPlayer = Player.White; // Белые всегда ходят первыми
            });
        };

        _onlineClient.OnMoveReceived += (move) =>
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                _board.Execute(move);
                RefreshGrid();
                FinishTurn();
                if (!IsGameOver) Status = "Ваш ход!";
            });
        };

        try
        {
            await _onlineClient.ConnectAsync();
            await _onlineClient.FindGame();
        }
        catch (Exception ex)
        {
            Status = $"Ошибка: {ex.Message}";
        }
    }

    private void ReturnToMenu()
    {
        IsStartScreen = true;
        IsGameOver = false;
        Status = "Выберите режим";
        _onlineClient = null;
    }

    //  ВЫХОД ИЗ ПРИЛОЖЕНИЯ 
    private void ExitApp()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow?.Close();
        }
    }

    //  СОЗДАНИЕ СЕТИ VIEWMODEL КЛЕТОК 
    private void CreateGrid()
    {
        var newGrid = Enumerable.Range(0, Board.Size)
            .SelectMany(r => Enumerable.Range(0, Board.Size)
                .Select(c => new CellViewModel(_board[r, c], r, c)))
            .ToArray();
        _grid = newGrid;
        OnPropertyChanged(nameof(Grid)); // Уведомляем UI об обновлении
    }

    //  ОБРАБОТЧИК КЛИКА ПО КЛЕТКЕ - ОСНОВНАЯ ЛОГИКА ИГРЫ 
    private async void OnCellClick(CellViewModel? cellVm)
    {
        if (cellVm == null || IsStartScreen || IsGameOver) return;

        //  БЛОКИРОВКА ХОДОВ ЧЕЛОВЕКА ПО ВРЕМЕНИ ДУМАНИЯ ИИ 
        if (GameMode == GameMode.HumanVsAI && CurrentPlayer == Player.Black) return;

        // Блокировка для Онлайн режима
        if (GameMode == GameMode.Online)
        {
            // Не даем ходить, пока не подключится второй игрок
            if (!_isOnlineGameActive) return;
            // Не даем ходить, если не наш цвет или не наша очередь
            if (_onlineClient == null || CurrentPlayer != _onlineClient.MyColor) return;
        }

        int row = cellVm.Row;
        int col = cellVm.Col;

        //  ПОПЫТКА СОВЕРШИТЬ ХОД (ЕСЛИ ШАШКА ВЫБРАНА) 
        if (Selected is { } sel)
        {
            var potentialMove = new Move(sel.row, sel.col, row, col);
            var realMove = GetLegalMoveMatch(potentialMove);

            if (realMove != null)
            {
                //  ВЫПОЛНЯЕМ ЛЕГАЛЬНЫЙ ХОД 
                _board.Execute(realMove);
                RefreshGrid();

                // Отправка хода на сервер
                if (GameMode == GameMode.Online && _onlineClient != null)
                {
                    await _onlineClient.SendMove(realMove);
                }

                //  ЛОГИКА МНОЖЕСТВЕННОГО ВЗЯТИЯ 
                // Ключевой момент русских шашек: если можно продолжить бить - продолжаем!
                if (realMove.IsCapture && CanCaptureMore(realMove.ToRow, realMove.ToCol))
                {
                    // Если было взятие и можно бить еще ЭТОЙ ЖЕ фигурой:
                    _chainPiece = (realMove.ToRow, realMove.ToCol);
                    Selected = _chainPiece; // Автовыбор этой фигуры
                    Status = "Рубите дальше!";
                    // Не переключаем игрока, ждем следующий клик
                }
                else
                {
                    // Ход закончен (либо не взятие, либо больше некого бить)
                    FinishTurn();
                }
                return;
            }
        }

        //  ВЫБОР ШАШКИ ДЛЯ ХОДА 
        var piece = _board[row, col];
        if (piece?.Owner == CurrentPlayer)
        {
            // Если мы в серии взятий, разрешаем выбрать ТОЛЬКО активную фигуру
            if (_chainPiece != null)
            {
                if (_chainPiece.Value.row == row && _chainPiece.Value.col == col)
                {
                    Selected = (row, col);
                }
                // Иначе игнорируем клик по другим шашкам
            }
            else
            {
                Selected = (row, col); // Обычный выбор шашки
            }
        }
        else
        {
            // Клик в пустоту или во врага снимает выделение
            if (_chainPiece == null) Selected = null;
        }
    }

    //  ЗАВЕРШЕНИЕ ХОДА И ПЕРЕДАЧА ОЧЕРЕДИ 
    private async void FinishTurn()
    {
        _chainPiece = null; // Сбрасываем цепочку взятий
        Selected = null;
        SwitchPlayer();
        CheckGameEnd();

        //  АВТОХОД ИИ ПОСЛЕ ХОДА ЧЕЛОВЕКА 
        if (!IsGameOver && GameMode == GameMode.HumanVsAI && CurrentPlayer == Player.Black)
        {
            await Task.Delay(300);
            await MakeAIMove(); 
        }
    }

    //  ПРОВЕРКА ВОЗМОЖНОСТИ ПРОДОЛЖИТЬ ВЗЯТИЕ 
    private bool CanCaptureMore(int r, int c)
    {
        return _board.GetLegalMoves(r, c, CurrentPlayer).Any(m => m.IsCapture);
    }

    //  ЛОГИКА ХОДА ИСКУССТВЕННОГО ИНТЕЛЛЕКТА 
    // Поддерживает множественные взятия в цикле
    private async Task MakeAIMove()
    {
        bool turnInProgress = true;
        while (turnInProgress && !IsGameOver)
        {
            // Получаем лучший ход. Если мы в серии (_chainPiece != null),
            // GetAllLegalMoves сам вернет только продолжение атаки.
            var move = GetBestMove(Player.Black, depth: 2);
            if (move != null)
            {
                _board.Execute(move);
                RefreshGrid();

                // Проверяем продолжение атаки
                if (move.IsCapture && CanCaptureMore(move.ToRow, move.ToCol))
                {
                    _chainPiece = (move.ToRow, move.ToCol);
                    await Task.Delay(400);
                }
                else
                {
                    _chainPiece = null;
                    turnInProgress = false;
                    SwitchPlayer();
                    CheckGameEnd();
                }
            }
            else
            {
                turnInProgress = false;
            }
        }
    }

    //  ПРОСТОЙ ИИ: СЛУЧАЙНЫЙ ВЫБОР ИЗ ЛЕГАЛЬНЫХ ХОДОВ 
    private Move? GetBestMove(Player aiPlayer, int depth)
    {
        // GetAllLegalMoves теперь учитывает _chainPiece
        var moves = GetAllLegalMoves(aiPlayer).ToList();
        if (!moves.Any()) return null;

        var random = new Random();
        var captureMoves = moves.Where(m => m.IsCapture).ToList();

        //  ПРИОРИТЕТ ВЗЯТИЯМ (УЖЕ УЧТЕН В GetAllLegalMoves, НО ДЛЯ НАДЕЖНОСТИ) 
        var candidates = captureMoves.Any() ? captureMoves : moves;
        return candidates[random.Next(candidates.Count)];
    }

    //  ФИЛЬТРАЦИЯ ЛЕГАЛЬНЫХ ХОДОВ С УЧЕТОМ ПРАВИЛ 
    private IEnumerable<Move> GetAllLegalMoves(Player player)
    {
        // 1. Если мы в процессе множественного взятия, доступны ходы ТОЛЬКО текущей фигурой
        if (_chainPiece != null)
        {
            var (r, c) = _chainPiece.Value;
            // И только ходы-взятия
            return _board.GetLegalMoves(r, c, player).Where(m => m.IsCapture);
        }

        // 2. Обычный сбор ходов
        var allMoves = _board.GetAllLegalMoves(player);

        // 3. ПРАВИЛО "БИТЬ ОБЯЗАТЕЛЬНО" - ОСНОВНОЙ ПРИНЦИП ШАШЕК
        var captures = allMoves.Where(m => m.IsCapture).ToList();
        if (captures.Any())
        {
            return captures; // Только взятия, если они возможны
        }
        return allMoves; // Иначе простые ходы
    }

    //  ПОИСК СОВПАДЕНИЯ КЛИКА С ЛЕГАЛЬНЫМ ХОДОМ 
    private Move? GetLegalMoveMatch(Move target)
    {
        // Ищем совпадение среди легальных ходов (которые уже отфильтрованы по правилам)
        var legalMoves = GetAllLegalMoves(CurrentPlayer);
        return legalMoves.FirstOrDefault(m =>
            m.FromRow == target.FromRow &&
            m.FromCol == target.FromCol &&
            m.ToRow == target.ToRow &&
            m.ToCol == target.ToCol);
    }

    //  ПЕРЕКЛЮЧЕНИЕ ОЧЕРЕДИ ИГРОКА 
    private void SwitchPlayer()
    {
        CurrentPlayer = CurrentPlayer == Player.White ? Player.Black : Player.White;
        Status = $"Ход {(CurrentPlayer == Player.White ? "белых" : "чёрных")}";
    }

    //  ОБНОВЛЕНИЕ UI ПОСЛЕ ХОДА 
    private void RefreshGrid()
    {
        for (int r = 0; r < Board.Size; r++)
            for (int c = 0; c < Board.Size; c++)
                _grid[r * Board.Size + c].Piece = _board[r, c];
    }

    //  ПРОВЕРКА УСЛОВИЙ ОКОНЧАНИЯ ИГРЫ 
    private void CheckGameEnd()
    {
        // Важно сбросить _chainPiece перед проверкой, чтобы посчитать все ходы
        // Но здесь мы вызываем CheckGameEnd только при смене хода, так что _chainPiece уже null

        bool whiteHasMoves = HasAnyMove(Player.White);
        bool blackHasMoves = HasAnyMove(Player.Black);

        if (!whiteHasMoves && !blackHasMoves)
        {
            Status = "Ничья!";
            IsGameOver = true;
        }
        else if (CurrentPlayer == Player.White && !whiteHasMoves)
        {
            Status = "Победа чёрных!";
            IsGameOver = true;
        }
        else if (CurrentPlayer == Player.Black && !blackHasMoves)
        {
            Status = "Победа белых!";
            IsGameOver = true;
        }
    }

    //  ПОМОЩНИК: ПРОВЕРКА НАЛИЧИЯ ХОДОВ У ИГРОКА 
    private bool HasAnyMove(Player p)
    {
        // Для проверки конца игры нам нужны ВСЕ возможные ходы, игнорируя текущую цепочку (она для UI)
        return _board.GetAllLegalMoves(p).Any();
    }
}
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CheckersGame.Models;
using CheckersGame.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

    // Свойства для Лобби
    [ObservableProperty]
    private bool _isLobbyScreen = false;

    [ObservableProperty]
    private string _lobbyStatus = "Подключение...";

    [ObservableProperty]
    private string _joinGameId = "";

    [ObservableProperty]
    private bool _isCreatingPrivate = false;

    // Новое свойство — видимость окна с правилами
    [ObservableProperty]
    private bool _isRulesVisible = false;

    public ObservableCollection<GameRoomDto> Lobbies { get; } = new();

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
    private IReadOnlyList<CellViewModel> _grid = Array.Empty<CellViewModel>();  // ← ИСПРАВЛЕНИЕ CS8618
    public IReadOnlyList<CellViewModel> Grid => _grid;

    // Метод переключения видимости правил
    private void ToggleRules()
    {
    IsRulesVisible = !IsRulesVisible;
    }

    //  КОМАНДЫ ДЛЯ UI 
    public ICommand CellCommand { get; }
    public ICommand StartHumanVsHumanCommand { get; }
    public ICommand StartHumanVsAICommand { get; }
    
    // Команды онлайна
    public ICommand StartOnlineGameCommand { get; } // Теперь открывает лобби
    public ICommand OpenOnlineLobbyCommand { get; }
    public ICommand CreateGameCommand { get; }
    public ICommand JoinGameCommand { get; }
    public ICommand JoinSelectedGameCommand { get; }
    public ICommand RefreshLobbiesCommand { get; }

    public ICommand ReturnToMenuCommand { get; }
    public ICommand ExitCommand { get; }

    // Новая команда для открытия/закрытия подсказки
    public ICommand ToggleRulesCommand { get; }
    

    //  ИНИЦИАЛИЗАЦИЯ VIEWMODEL 
    public GameViewModel()
    {
        _board = new Board();
        CreateGrid(); // Создаем начальную сетку клеток

        //  НАСТРОЙКА КОМАНД 
        CellCommand = new RelayCommand<CellViewModel>(OnCellClick);
        StartHumanVsHumanCommand = new RelayCommand(() => StartGame(GameMode.HumanVsHuman));
        StartHumanVsAICommand = new RelayCommand(() => StartGame(GameMode.HumanVsAI));
        
        // Настройка команд лобби
        OpenOnlineLobbyCommand = new RelayCommand(OpenOnlineLobby);
        StartOnlineGameCommand = new RelayCommand(OpenOnlineLobby); // Кнопка меню теперь ведет в лобби
        CreateGameCommand = new RelayCommand(CreateOnlineGame);
        JoinGameCommand = new RelayCommand(() => JoinOnlineGame(JoinGameId));
        JoinSelectedGameCommand = new RelayCommand<string>(JoinOnlineGame);
        RefreshLobbiesCommand = new RelayCommand(RequestLobbies);

        ReturnToMenuCommand = new RelayCommand(ReturnToMenu);
        ExitCommand = new RelayCommand(ExitApp);
        ToggleRulesCommand = new RelayCommand(ToggleRules);
    }

    //  СТАРТ НОВОЙ ЛОКАЛЬНОЙ ИГРЫ 
    private void StartGame(GameMode mode)
    {
        GameMode = mode;
        IsStartScreen = false;
        IsLobbyScreen = false;
        IsGameOver = false;
        _onlineClient = null;
        
        //  ПЕРЕЗАПУСК ИГРОВОГО СОСТОЯНИЯ 
        StartNewBoard();
    }

    private void StartNewBoard()
    {
        _board = new Board();
        _chainPiece = null;
        CreateGrid();
        CurrentPlayer = Player.White;
        Selected = null;
        if (GameMode != GameMode.Online) Status = "Ход белых";
    }

    // --- ЛОГИКА ЛОББИ ---

    private async void OpenOnlineLobby()
    {
        IsStartScreen = false;
        IsLobbyScreen = true;
        IsGameOver = false;
        LobbyStatus = "Подключение к серверу...";
        Lobbies.Clear();

        _onlineClient = new OnlineGameClient();
        SetupOnlineEvents();

        try
        {
            await _onlineClient.ConnectAsync();
            LobbyStatus = "Сервер доступен. Выберите или создайте игру.";
            await _onlineClient.RequestLobbyListAsync();
        }
        catch (Exception ex)
        {
            LobbyStatus = $"Ошибка подключения: {ex.Message}";
        }
    }

    private void SetupOnlineEvents()
    {
        if (_onlineClient == null) return;

        _onlineClient.OnLobbyListUpdated += (list) =>
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                Lobbies.Clear();
                foreach (var item in list) Lobbies.Add(item);
            });
        };

        _onlineClient.OnError += (msg) =>
        {
            Dispatcher.UIThread.Invoke(() => LobbyStatus = $"Ошибка: {msg}");
        };

        _onlineClient.OnWaiting += (id) =>
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                LobbyStatus = $"Игра создана. ID: {id}. Ожидание соперника...";
            });
        };

        _onlineClient.OnOpponentDisconnected += () =>
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                Status = "Соперник отключился. Победа!";
                IsGameOver = true;
                _isOnlineGameActive = false;
            });
        };

        _onlineClient.OnGameStarted += () =>
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                IsLobbyScreen = false;
                IsGameOver = false;
                _isOnlineGameActive = true;
                GameMode = GameMode.Online;

                StartNewBoard();

                var colorName = _onlineClient.MyColor == Player.White ? "Белыми" : "Чёрными";
                Status = $"Игра началась! Вы играете {colorName}.";
                CurrentPlayer = Player.White;
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
    }

    private async void CreateOnlineGame()
    {
        if (_onlineClient == null) return;
        LobbyStatus = "Создание комнаты...";
        await _onlineClient.CreateGameAsync(IsCreatingPrivate);
    }

    private async void JoinOnlineGame(string? gameId)
    {
        if (string.IsNullOrWhiteSpace(gameId) || _onlineClient == null) return;
        LobbyStatus = $"Подключение к {gameId}...";
        await _onlineClient.JoinGameAsync(gameId);
    }

    private async void RequestLobbies()
    {
        if (_onlineClient != null) await _onlineClient.RequestLobbyListAsync();
    }

    // --- КОНЕЦ ЛОГИКИ ЛОББИ ---

    private void ReturnToMenu()
    {
        IsStartScreen = true;
        IsLobbyScreen = false;
        IsGameOver = false;
        Status = "Выберите режим";
        _chainPiece = null;

        if (_onlineClient != null)
        {
            // Исправление CS1061: Вызов нового метода DisconnectAsync
            _onlineClient.DisconnectAsync();  // Асинхронно, без await, чтобы не блокировать UI
            _onlineClient = null;
            _isOnlineGameActive = false;
        }
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
        // Добавлена проверка IsLobbyScreen
        if (cellVm == null || IsStartScreen || IsLobbyScreen || IsGameOver) return;

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
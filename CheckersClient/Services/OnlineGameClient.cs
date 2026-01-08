using Microsoft.AspNetCore.SignalR.Client;
using CheckersGame.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CheckersGame.Services;

public class OnlineGameClient
{
    private HubConnection? _hubConnection;
    private string? _gameId;
    public Player MyColor { get; private set; }

    // События
    public event Action<string>? OnWaiting;
    public event Action? OnGameStarted;
    public event Action<Move>? OnMoveReceived;
    public event Action<List<GameRoomDto>>? OnLobbyListUpdated;
    public event Action<string>? OnError;
    public event Action? OnOpponentDisconnected;

    public async Task ConnectAsync()
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl("http://localhost:5000/gamehub") 
            .WithAutomaticReconnect()
            .Build();

        // Сервер назначил ID комнаты (для создателя)
        _hubConnection.On<string>("WaitingForOpponent", (id) =>
        {
            _gameId = id;
            OnWaiting?.Invoke(id);
        });

        // Старт игры: получение ID и цвета фигуры
        _hubConnection.On<string, int>("GameStarted", (id, colorInt) =>
        {
            _gameId = id;
            MyColor = (Player)colorInt;
            OnGameStarted?.Invoke();
        });

        // Получение хода от оппонента
        _hubConnection.On<Move>("ReceiveMove", (move) =>
        {
            OnMoveReceived?.Invoke(move);
        });

        // Получение списка (используется при NotifyLobbyUpdate старой версии)
        _hubConnection.On<List<GameRoomDto>>("LobbyListUpdated", (lobbies) =>
        {
            OnLobbyListUpdated?.Invoke(lobbies);
        });

        // АВТО-ОБНОВЛЕНИЕ: Сервер сообщает, что данные в лобби изменились
        _hubConnection.On("LobbyDataChanged", async () =>
        {
            await RequestLobbyListAsync();
        });

        // Ошибки (например, комната не найдена)
        _hubConnection.On<string>("ErrorMessage", (msg) =>
        {
            OnError?.Invoke(msg);
        });
        
        // Оппонент вышел из игры
        _hubConnection.On("OpponentDisconnected", () =>
        {
            OnOpponentDisconnected?.Invoke();
        });

        await _hubConnection.StartAsync();
    }

    // --- Методы взаимодействия с сервером ---
    
    public async Task CreateGameAsync(bool isPrivate)
    {
        if (_hubConnection != null)
            await _hubConnection.InvokeAsync("CreateGame", isPrivate);
    }

    public async Task JoinGameAsync(string gameId)
    {
        if (_hubConnection != null)
            await _hubConnection.InvokeAsync("JoinGame", gameId);
    }

    public async Task RequestLobbyListAsync()
    {
        if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
        {
            try 
            {
                // Запрашиваем персональный список (без своих комнат)
                var list = await _hubConnection.InvokeAsync<List<GameRoomDto>>("GetLobbies");
                OnLobbyListUpdated?.Invoke(list);
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"Ошибка при получении списка: {ex.Message}");
            }
        }
    }

    public async Task SendMove(Move move)
    {
        if (_hubConnection != null && _gameId != null)
            await _hubConnection.InvokeAsync("SendMove", _gameId, move);
    }

    // ← НОВЫЙ МЕТОД ДЛЯ ОТКЛЮЧЕНИЯ (ИСПРАВЛЕНИЕ CS1061)
    public async Task DisconnectAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.StopAsync();
            _hubConnection = null;
        }
    }
}
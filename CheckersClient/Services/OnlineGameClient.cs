using Microsoft.AspNetCore.SignalR.Client;
using CheckersGame.Models;
using System;
using System.Threading.Tasks;

namespace CheckersGame.Services;

public class OnlineGameClient
{
    private HubConnection? _hubConnection;
    private string? _gameId;
    public Player MyColor { get; private set; }

    // События для ViewModel
    public event Action<string>? OnWaiting;
    public event Action? OnGameStarted;
    public event Action<Move>? OnMoveReceived;

    public async Task ConnectAsync()
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl("http://localhost:5000/gamehub") // Убедитесь, что порт совпадает с сервером
            .WithAutomaticReconnect()
            .Build();

        // Подписки на сообщения от сервера
        _hubConnection.On<string>("WaitingForOpponent", (id) =>
        {
            _gameId = id;
            OnWaiting?.Invoke(id);
        });

        _hubConnection.On<string, int>("GameStarted", (id, colorInt) =>
        {
            _gameId = id;
            MyColor = (Player)colorInt; // 0 - White, 1 - Black
            OnGameStarted?.Invoke();
        });

        _hubConnection.On<Move>("ReceiveMove", (move) =>
        {
            OnMoveReceived?.Invoke(move);
        });

        await _hubConnection.StartAsync();
    }

    public async Task FindGame()
    {
        if (_hubConnection != null)
            await _hubConnection.InvokeAsync("FindGame");
    }

    public async Task SendMove(Move move)
    {
        if (_hubConnection != null && _gameId != null)
            await _hubConnection.InvokeAsync("SendMove", _gameId, move);
    }
}
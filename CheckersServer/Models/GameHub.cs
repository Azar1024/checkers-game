using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using CheckersGame.Models;

public class GameHub : Hub
{
    private class GameRoom
    {
        public string Id { get; set; }
        public string Player1Id { get; set; } 
        public string? Player2Id { get; set; } 
        public bool IsPrivate { get; set; }
        public bool IsFull => !string.IsNullOrEmpty(Player2Id);
    }

    private static ConcurrentDictionary<string, GameRoom> _rooms = new();

    public async Task<string> CreateGame(bool isPrivate)
    {
        var existingRoom = _rooms.Values.FirstOrDefault(r => r.Player1Id == Context.ConnectionId);
        if (existingRoom != null)
        {
            _rooms.TryRemove(existingRoom.Id, out _);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, existingRoom.Id);
            
            if (existingRoom.Player2Id != null)
            {
                await Clients.Client(existingRoom.Player2Id).SendAsync("OpponentDisconnected");
            }
        }

        var gameId = Guid.NewGuid().ToString().Substring(0, 5).ToUpper();
        
        var room = new GameRoom
        {
            Id = gameId,
            Player1Id = Context.ConnectionId,
            Player2Id = null,
            IsPrivate = isPrivate
        };

        if (_rooms.TryAdd(gameId, room))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, gameId);
            await Clients.Caller.SendAsync("WaitingForOpponent", gameId);
            
            // После создания комнаты обновляем список у ВСЕХ
            await NotifyLobbyUpdate();
            
            return gameId;
        }
        
        throw new HubException("Failed to create room.");
    }

    public async Task<List<GameRoomDto>> GetLobbies()
    {
        return _rooms.Values
            .Where(r => !r.IsPrivate && !r.IsFull && r.Player1Id != Context.ConnectionId)
            .Select(r => new GameRoomDto 
            { 
                Id = r.Id, 
                IsPrivate = r.IsPrivate,
                Name = $"Комната {r.Id}" 
            })
            .ToList();
    }

    public async Task JoinGame(string gameId)
    {
        gameId = gameId?.ToUpper();

        if (_rooms.TryGetValue(gameId, out var room))
        {
            if (room.IsFull)
            {
                await Clients.Caller.SendAsync("ErrorMessage", "Комната переполнена.");
                return;
            }

            room.Player2Id = Context.ConnectionId;
            await Groups.AddToGroupAsync(Context.ConnectionId, gameId);

            await Clients.Client(room.Player1Id).SendAsync("GameStarted", gameId, 0);
            await Clients.Client(Context.ConnectionId).SendAsync("GameStarted", gameId, 1);

            await NotifyLobbyUpdate();
        }
        else
        {
            await Clients.Caller.SendAsync("ErrorMessage", "Комната не найдена.");
        }
    }

    private async Task NotifyLobbyUpdate()
    {
        // Оповещаем всех клиентов, что данные в лобби изменились.
        // Клиент, получив это событие, должен вызвать GetLobbies() через прокси.
        await Clients.All.SendAsync("LobbyDataChanged");
    }

    public async Task SendMove(string gameId, MoveDto move)
    {
        await Clients.OthersInGroup(gameId).SendAsync("ReceiveMove", move);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var room = _rooms.Values.FirstOrDefault(r => r.Player1Id == Context.ConnectionId || r.Player2Id == Context.ConnectionId);
        
        if (room != null)
        {
            _rooms.TryRemove(room.Id, out _);
            await Clients.Group(room.Id).SendAsync("OpponentDisconnected");
            await NotifyLobbyUpdate();
        }
        
        await base.OnDisconnectedAsync(exception);
    }
}
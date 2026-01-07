using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

public class GameHub : Hub
{
    // Храним игры: GameId -> (Player1Id, Player2Id)
    private static ConcurrentDictionary<string, List<string>> _games = new();

    public async Task FindGame()
    {
        // Простая логика: ищем игру, где 1 игрок, иначе создаем новую
        var existingGame = _games.FirstOrDefault(x => x.Value.Count == 1);

        if (existingGame.Key != null)
        {
            // Подключаемся к существующей
            var gameId = existingGame.Key;
            _games[gameId].Add(Context.ConnectionId);
            
            await Groups.AddToGroupAsync(Context.ConnectionId, gameId);
            
            // Уведомляем обоих, что игра началась. Второму (присоединившемуся) даем Черные (Player 1 (enum))
            // 0 = White, 1 = Black
            await Clients.Client(existingGame.Value[0]).SendAsync("GameStarted", gameId, 0); // Ты Белый
            await Clients.Client(Context.ConnectionId).SendAsync("GameStarted", gameId, 1);  // Ты Черный
        }
        else
        {
            // Создаем новую
            var gameId = Guid.NewGuid().ToString().Substring(0, 5);
            _games.TryAdd(gameId, new List<string> { Context.ConnectionId });
            
            await Groups.AddToGroupAsync(Context.ConnectionId, gameId);
            await Clients.Caller.SendAsync("WaitingForOpponent", gameId);
        }
    }

    public async Task SendMove(string gameId, MoveDto move)
    {
        // Пересылаем ход группе, КРОМЕ отправителя
        await Clients.OthersInGroup(gameId).SendAsync("ReceiveMove", move);
    }
}

// DTO для передачи данных (совпадает со структурой Move в клиенте)
public record MoveDto(int FromRow, int FromCol, int ToRow, int ToCol, bool IsCapture);
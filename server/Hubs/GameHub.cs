using Microsoft.AspNetCore.SignalR;

public class GameHub : Hub
{
    public async Task JoinRoom(string gameId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, gameId);
    }

    public async Task JoinAsHost(string gameId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, gameId);
        await Groups.AddToGroupAsync(Context.ConnectionId, $"{gameId}-host");
    }

    public async Task LeaveRoom(string gameId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, gameId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"{gameId}-host");
    }

    public async Task NextQuestion(string gameId, object payload)
    {
        await Clients.OthersInGroup(gameId).SendAsync("nextQuestion", payload);
    }

    public async Task EndGame(string gameId, object payload)
    {
        await Clients.OthersInGroup(gameId).SendAsync("endGame", payload);
    }

    public async Task OrderRanking(string gameId, object payload)
    {
        await Clients.Group(gameId).SendAsync("orderRanking", payload);
    }
}

using Microsoft.AspNetCore.SignalR;

public class GameHub : Hub
{
    public async Task JoinRoom(string gameId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, gameId);
    }

    public async Task LeaveRoom(string gameId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, gameId);
    }

    public async Task NextQuestion(string gameId, object payload)
    {
        await Clients.Group(gameId).SendAsync("nextQuestion", payload);
    }

    public async Task EndGame(string gameId, object payload)
    {
        await Clients.Group(gameId).SendAsync("endGame", payload);
    }

    public async Task OrderRanking(string gameId, object payload)
    {
        await Clients.Group(gameId).SendAsync("orderRanking", payload);
    }
}

public class Profile
{
    public string Id { get; set; } = ""; // Auth0 sub
    public string Email { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Picture { get; set; }
    public string? TeamName { get; set; }
    public int CurrentPoints { get; set; } = 0;
    public int? CurrentGameId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Game? CurrentGame { get; set; }
    public List<Response> Responses { get; set; } = [];
}

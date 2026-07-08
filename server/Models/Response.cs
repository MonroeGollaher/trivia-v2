public class Response
{
    public int Id { get; set; }
    public string TeamId { get; set; } = "";
    public int QuestionId { get; set; }
    public bool Approved { get; set; } = false;
    public int Wager { get; set; }
    public string Answer { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Profile Team { get; set; } = null!;
    public Question Question { get; set; } = null!;
}

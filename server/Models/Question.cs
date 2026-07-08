public class Question
{
    public int Id { get; set; }
    public string Category { get; set; } = "";
    public string Text { get; set; } = "";
    public string Answer { get; set; } = "";
    public List<string> WrongAnswers { get; set; } = [];
    public int GameId { get; set; }

    public Game Game { get; set; } = null!;
    public List<Response> Responses { get; set; } = [];
}

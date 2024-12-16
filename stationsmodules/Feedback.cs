using System;

public class Feedback
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string Message { get; set; }
    public string FeedbackMessage { get; set; }
    public DateTime Date { get; set; }

    public long StationId { get; set; }
    public string StationName { get; set; }
}

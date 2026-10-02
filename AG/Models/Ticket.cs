using System.Text.Json.Serialization;

namespace AG.Models;

public class Ticket
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Priority Priority { get; set; } = Priority.MEDIUM;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TicketType Type { get; set; } = TicketType.TASK;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TicketStatus Status { get; set; } = TicketStatus.TODO;

    public int StoryPoints { get; set; } = 3;

    public string Assignee { get; set; } = "Non assigné";

    public bool IsClosed { get; set; }

    public int? SprintId { get; set; }
    public Sprint? Sprint { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Ticket() { }

    [JsonConstructor]
    public Ticket(
        int id,
        string title,
        Priority priority,
        bool isClosed = false,
        string? description = null,
        int? sprintId = null,
        TicketStatus status = TicketStatus.TODO,
        TicketType type = TicketType.TASK,
        int storyPoints = 3,
        string assignee = "Non assigné")
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Le titre ne peut pas être vide.", nameof(title));
        }

        Id = id;
        Title = title.Trim();
        Priority = priority;
        IsClosed = isClosed;
        Description = description;
        SprintId = sprintId;
        Status = isClosed ? TicketStatus.DONE : status;
        Type = type;
        StoryPoints = storyPoints;
        Assignee = string.IsNullOrWhiteSpace(assignee) ? "Non assigné" : assignee.Trim();
    }

    public void Close()
    {
        IsClosed = true;
        Status = TicketStatus.DONE;
    }

    public void Reopen()
    {
        IsClosed = false;
        if (Status == TicketStatus.DONE)
        {
            Status = TicketStatus.TODO;
        }
    }

    public override string ToString()
    {
        return $"[{Type} | {Priority}] #{Id} - {Title} ({Status}, {StoryPoints} pts, {Assignee})";
    }
}

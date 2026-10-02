using System.Text.Json.Serialization;

namespace AG.Models;

public record CreateTicketDto(
    string Title,
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    Priority Priority,
    string? Description = null,
    int? SprintId = null,
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    TicketType Type = TicketType.TASK,
    int StoryPoints = 3,
    string Assignee = "Non assigné"
);

public record CreateSprintDto(
    string Name,
    string? Goal = null
);

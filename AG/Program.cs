using System.Text.Json.Serialization;
using AG.ConsoleUi;
using AG.Models;
using AG.Services;

var service = new TicketService();

if (args.Contains("--api"))
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.ConfigureHttpJsonOptions(options =>
    {
        options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

    builder.Services.AddSingleton(service);

    var app = builder.Build();

    // GET /tickets : Liste tous les tickets (avec filtres optionnels : ?sprintId=1, ?type=BUG, ?search=bce)
    app.MapGet("/tickets", (TicketService s, bool? openOnly, int? sprintId, string? search, TicketType? type, string? assignee) =>
    {
        if (!string.IsNullOrWhiteSpace(search) || type.HasValue || !string.IsNullOrWhiteSpace(assignee))
        {
            return Results.Ok(s.SearchTickets(search ?? string.Empty, sprintId, type, assignee));
        }

        if (sprintId.HasValue)
        {
            return Results.Ok(s.GetTicketsBySprint(sprintId.Value));
        }

        if (openOnly == true)
        {
            return Results.Ok(s.GetOpenTicketsSortedByPriority());
        }

        return Results.Ok(s.GetAllTickets());
    });

    // GET /tickets/open : Liste uniquement les tickets ouverts triés par priorité
    app.MapGet("/tickets/open", (TicketService s) =>
    {
        return Results.Ok(new
        {
            TotalOpen = s.GetOpenTicketsCount(),
            Tickets = s.GetOpenTicketsSortedByPriority()
        });
    });

    // GET /tickets/{id} : Récupérer un ticket par ID
    app.MapGet("/tickets/{id:int}", (int id, TicketService s) =>
    {
        var ticket = s.GetTicketById(id);
        return ticket is null
            ? Results.NotFound(new { Message = $"Ticket #{id} introuvable." })
            : Results.Ok(ticket);
    });

    // POST /tickets : Créer un ticket
    app.MapPost("/tickets", (CreateTicketDto dto, TicketService s) =>
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            return Results.BadRequest(new { Message = "Le titre est obligatoire et ne peut pas être vide." });
        }

        var ticket = s.CreateTicket(
            dto.Title,
            dto.Priority,
            dto.Description,
            dto.SprintId,
            TicketStatus.TODO,
            dto.Type,
            dto.StoryPoints,
            dto.Assignee);

        return Results.Created($"/tickets/{ticket.Id}", ticket);
    });

    // PUT /tickets/{id}/status : Changer le statut Kanban (TODO, IN_PROGRESS, DONE)
    app.MapPut("/tickets/{id:int}/status", (int id, TicketStatus status, TicketService s) =>
    {
        var updated = s.UpdateTicketStatus(id, status);
        return updated
            ? Results.Ok(new { Message = $"Statut du ticket #{id} mis à jour : {status}" })
            : Results.NotFound(new { Message = $"Ticket #{id} introuvable." });
    });

    // PUT /tickets/{id}/move-sprint : Déplacer un ticket vers un sprint (ou null pour le backlog)
    app.MapPut("/tickets/{id:int}/move-sprint", (int id, int? sprintId, TicketService s) =>
    {
        var moved = s.MoveTicketToSprint(id, sprintId);
        return moved
            ? Results.Ok(new { Message = $"Ticket #{id} déplacé vers {(sprintId.HasValue ? $"le sprint #{sprintId}" : "le backlog")}." })
            : Results.NotFound(new { Message = $"Ticket #{id} introuvable." });
    });

    // PUT /tickets/{id}/close : Clôturer un ticket
    app.MapPut("/tickets/{id:int}/close", (int id, TicketService s) =>
    {
        var closed = s.CloseTicket(id);
        if (!closed)
        {
            return Results.NotFound(new { Message = $"Ticket #{id} introuvable." });
        }

        var ticket = s.GetTicketById(id);
        return Results.Ok(new { Message = $"Ticket #{id} clôturé avec succès.", Ticket = ticket });
    });

    // DELETE /tickets/{id} : Supprimer un ticket
    app.MapDelete("/tickets/{id:int}", (int id, TicketService s) =>
    {
        var deleted = s.DeleteTicket(id);
        return deleted
            ? Results.NoContent()
            : Results.NotFound(new { Message = $"Ticket #{id} introuvable." });
    });

    // GET /sprints : Lister tous les sprints avec leurs tickets
    app.MapGet("/sprints", (TicketService s) =>
    {
        return Results.Ok(s.GetAllSprints());
    });

    // POST /sprints : Créer un nouveau sprint
    app.MapPost("/sprints", (CreateSprintDto dto, TicketService s) =>
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return Results.BadRequest(new { Message = "Le nom du sprint ne peut pas être vide." });
        }

        var sprint = s.CreateSprint(dto.Name, dto.Goal);
        return Results.Created($"/sprints/{sprint.Id}", sprint);
    });

    Console.WriteLine("API REST Jira démarrée sur les ports configurés.");
    app.Run();
}
else
{
    ConsoleApp.Run(service);
}

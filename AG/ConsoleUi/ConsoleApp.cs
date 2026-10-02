using AG.Models;
using AG.Services;

namespace AG.ConsoleUi;

public static class ConsoleApp
{
    public static void Run(TicketService service)
    {
        bool running = true;

        while (running)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("=== GESTIONNAIRE DE TICKETS AGILE / JIRA (SQLITE) ===");
            Console.ResetColor();
            Console.WriteLine("1. Afficher les tickets ouverts (triés par priorité)");
            Console.WriteLine("2. Afficher tous les tickets (avec détails & Sprints)");
            Console.WriteLine("3. Créer un nouveau ticket (Titre, Description, Sprint)");
            Console.WriteLine("4. Clôturer un ticket");
            Console.WriteLine("5. Changer le statut d'un ticket (TODO / IN_PROGRESS / DONE)");
            Console.WriteLine("6. Assigner / Déplacer un ticket dans un Sprint");
            Console.WriteLine("7. Créer un nouveau Sprint");
            Console.WriteLine("8. Lister les Sprints");
            Console.WriteLine("9. Supprimer un ticket");
            Console.WriteLine("0. Quitter");
            Console.Write("\nVotre choix : ");

            var choice = Console.ReadLine()?.Trim();

            Console.WriteLine();

            switch (choice)
            {
                case "1":
                    ListOpenTickets(service);
                    break;
                case "2":
                    ListAllTickets(service);
                    break;
                case "3":
                    CreateTicket(service);
                    break;
                case "4":
                    CloseTicket(service);
                    break;
                case "5":
                    ChangeStatus(service);
                    break;
                case "6":
                    AssignSprint(service);
                    break;
                case "7":
                    CreateSprint(service);
                    break;
                case "8":
                    ListSprints(service);
                    break;
                case "9":
                    DeleteTicket(service);
                    break;
                case "0":
                    running = false;
                    Console.WriteLine("Au revoir !");
                    break;
                default:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Option invalide, veuillez réessayer.");
                    Console.ResetColor();
                    break;
            }
        }
    }

    private static void ListOpenTickets(TicketService service)
    {
        var openTickets = service.GetOpenTicketsSortedByPriority();

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("--- TICKETS OUVERTS (PAR PRIORITÉ DÉCROISSANTE) ---");
        Console.ResetColor();

        if (openTickets.Count == 0)
        {
            Console.WriteLine("Aucun ticket ouvert.");
        }
        else
        {
            foreach (var ticket in openTickets)
            {
                PrintColoredTicket(ticket);
            }
        }

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\nTickets ouverts : {service.GetOpenTicketsCount()}");
        Console.ResetColor();
    }

    private static void ListAllTickets(TicketService service)
    {
        var allTickets = service.GetAllTickets();

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("--- TOUS LES TICKETS ---");
        Console.ResetColor();

        if (allTickets.Count == 0)
        {
            Console.WriteLine("Aucun ticket enregistré.");
            return;
        }

        foreach (var ticket in allTickets)
        {
            var sprintName = ticket.Sprint != null ? ticket.Sprint.Name : "Backlog";
            var status = $"[{ticket.Status} | {sprintName}]";
            PrintColoredTicket(ticket, status);
            if (!string.IsNullOrWhiteSpace(ticket.Description))
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"      └─ Description : {ticket.Description}");
                Console.ResetColor();
            }
        }
        Console.WriteLine($"\nTotal : {allTickets.Count} ticket(s) (dont {service.GetOpenTicketsCount()} ouvert(s))");
    }

    private static void CreateTicket(TicketService service)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("--- NOUVEAU TICKET ---");
        Console.ResetColor();

        string title;
        while (true)
        {
            Console.Write("Titre du ticket : ");
            var input = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(input))
            {
                title = input.Trim();
                break;
            }
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Le titre ne peut pas être vide.");
            Console.ResetColor();
        }

        Console.Write("Description (optionnel) : ");
        var description = Console.ReadLine()?.Trim();

        Priority priority;
        while (true)
        {
            Console.WriteLine("Priorité :");
            Console.WriteLine("  1. LOW");
            Console.WriteLine("  2. MEDIUM");
            Console.WriteLine("  3. HIGH");
            Console.Write("Votre choix (1-3, défaut = MEDIUM) : ");
            var pChoice = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(pChoice) || pChoice == "2")
            {
                priority = Priority.MEDIUM;
                break;
            }
            if (pChoice == "1")
            {
                priority = Priority.LOW;
                break;
            }
            if (pChoice == "3")
            {
                priority = Priority.HIGH;
                break;
            }

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Choix de priorité invalide.");
            Console.ResetColor();
        }

        // Choix du sprint
        int? sprintId = null;
        var sprints = service.GetAllSprints();
        if (sprints.Any())
        {
            Console.WriteLine("Assigner à un Sprint ?");
            Console.WriteLine("  0. Laisser dans le Backlog");
            foreach (var s in sprints)
            {
                Console.WriteLine($"  {s.Id}. {s.Name}");
            }
            Console.Write("ID du Sprint : ");
            if (int.TryParse(Console.ReadLine(), out int chosenSid) && sprints.Any(s => s.Id == chosenSid))
            {
                sprintId = chosenSid;
            }
        }

        try
        {
            var ticket = service.CreateTicket(title, priority, description, sprintId, TicketStatus.TODO);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\nTicket #{ticket.Id} créé avec succès ! ({ticket})");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Erreur lors de la création : {ex.Message}");
            Console.ResetColor();
        }
    }

    private static void ChangeStatus(TicketService service)
    {
        Console.Write("ID du ticket : ");
        if (!int.TryParse(Console.ReadLine(), out int id)) return;

        Console.WriteLine("Nouveau statut :");
        Console.WriteLine("  1. TODO");
        Console.WriteLine("  2. IN_PROGRESS");
        Console.WriteLine("  3. DONE");
        Console.Write("Choix : ");
        var choice = Console.ReadLine()?.Trim();

        var status = choice switch
        {
            "2" => TicketStatus.IN_PROGRESS,
            "3" => TicketStatus.DONE,
            _ => TicketStatus.TODO
        };

        if (service.UpdateTicketStatus(id, status))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Ticket #{id} passé à l'état {status}.");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Ticket introuvable.");
            Console.ResetColor();
        }
    }

    private static void AssignSprint(TicketService service)
    {
        Console.Write("ID du ticket à déplacer : ");
        if (!int.TryParse(Console.ReadLine(), out int id)) return;

        var sprints = service.GetAllSprints();
        Console.WriteLine("Choisissez la destination :");
        Console.WriteLine("  0. Backlog général");
        foreach (var s in sprints)
        {
            Console.WriteLine($"  {s.Id}. {s.Name}");
        }
        Console.Write("ID destination : ");
        if (int.TryParse(Console.ReadLine(), out int sid))
        {
            int? target = sid == 0 ? null : sid;
            service.MoveTicketToSprint(id, target);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Ticket déplacé !");
            Console.ResetColor();
        }
    }

    private static void CreateSprint(TicketService service)
    {
        Console.Write("Nom du Sprint (ex: Sprint 3) : ");
        var name = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(name)) return;

        Console.Write("Objectif du Sprint : ");
        var goal = Console.ReadLine();

        var sprint = service.CreateSprint(name, goal, SprintState.ACTIVE);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Sprint #{sprint.Id} '{sprint.Name}' créé avec succès !");
        Console.ResetColor();
    }

    private static void ListSprints(TicketService service)
    {
        var sprints = service.GetAllSprints();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("--- SPRINTS ---");
        Console.ResetColor();

        foreach (var s in sprints)
        {
            Console.WriteLine($"[#{s.Id}] {s.Name} ({s.State}) - {s.Tickets.Count} ticket(s)");
            if (!string.IsNullOrWhiteSpace(s.Goal))
            {
                Console.WriteLine($"      └─ Objectif : {s.Goal}");
            }
        }
    }

    private static void CloseTicket(TicketService service)
    {
        Console.Write("ID du ticket à clôturer : ");
        if (!int.TryParse(Console.ReadLine(), out int id)) return;

        if (service.CloseTicket(id))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Ticket #{id} clôturé avec succès.");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Ticket #{id} introuvable.");
            Console.ResetColor();
        }
    }

    private static void DeleteTicket(TicketService service)
    {
        Console.Write("ID du ticket à supprimer : ");
        if (!int.TryParse(Console.ReadLine(), out int id)) return;

        Console.Write($"Confirmer suppression ? (o/N) : ");
        var confirm = Console.ReadLine()?.Trim().ToLowerInvariant();
        if (confirm is "o" or "oui" or "y" or "yes")
        {
            service.DeleteTicket(id);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Ticket #{id} supprimé.");
            Console.ResetColor();
        }
    }

    private static void PrintColoredTicket(Ticket ticket, string? statusSuffix = null)
    {
        var color = ticket.Priority switch
        {
            Priority.HIGH => ConsoleColor.Red,
            Priority.MEDIUM => ConsoleColor.Yellow,
            Priority.LOW => ConsoleColor.Blue,
            _ => ConsoleColor.White
        };

        Console.ForegroundColor = color;
        Console.Write($"[{ticket.Priority,-6}] ");
        Console.ResetColor();
        Console.Write($"#{ticket.Id} - {ticket.Title}");

        if (!string.IsNullOrEmpty(statusSuffix))
        {
            Console.ForegroundColor = ticket.IsClosed ? ConsoleColor.DarkGray : ConsoleColor.Green;
            Console.Write($" {statusSuffix}");
            Console.ResetColor();
        }

        Console.WriteLine();
    }
}

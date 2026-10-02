# 🎫 Mini Gestionnaire de Tickets (.NET 10)

Cette application est une démonstration complète en C# / .NET 10 simulant un système de tickets de support (type Jira) sans base de données externe.

Elle propose désormais **3 interfaces au choix** qui partagent la même logique métier :
1. 🖥️ **Console interactive** (par défaut).
2. 🌐 **API REST** (Minimal API ASP.NET Core).
3. 💻 **Interface Bureau Native** (Avalonia UI XAML pour macOS / Windows / Linux).

---

## 📁 Structure de la Solution

```text
AG.sln
├── AG/                         # Projet Cœur, Console & Web API
│   ├── Models/
│   │   ├── Priority.cs         # Enum LOW, MEDIUM, HIGH
│   │   ├── Ticket.cs           # Modèle Ticket (Id, Title, Priority, IsClosed)
│   │   └── CreateTicketDto.cs  # DTO pour les requêtes POST JSON
│   ├── Services/
│   │   └── TicketService.cs    # Logique métier thread-safe
│   ├── ConsoleUi/
│   │   └── ConsoleApp.cs       # Menu et UI en ligne de commande
│   └── Program.cs              # Switch Console / API REST
│
└── AG.Desktop/                 # Projet Interface Bureau (Avalonia UI)
    ├── MainWindow.axaml        # Vue XAML (formulaire, liste, badges, filtres)
    ├── MainWindow.axaml.cs     # Code-behind interactif
    └── Program.cs              # Point d'entrée de l'application Desktop
```

---

## 💻 1. Lancer l'Application Bureau (Avalonia UI)

Une interface graphique moderne et native sur macOS, Windows et Linux :

```bash
dotnet run --project AG.Desktop
```

### Fonctionnalités de l'interface bureau :
- **Formulaire de création rapide** : saisie du titre et sélecteur de priorité (`LOW`, `MEDIUM`, `HIGH`).
- **Cartes de tickets avec badges colorés** : rouge pour `HIGH`, orange pour `MEDIUM`, bleu pour `LOW`.
- **Boutons d'action directs sur chaque ticket** : « Clôturer » et « Supprimer ».
- **Filtres en 1 clic** : basculer entre « Ouverts seulement » et « Tous les tickets ».
- **Tableau de bord dynamique** : compteur de tickets ouverts et total en temps réel.

---

## 🖥️ 2. Lancer la version Console interactive

Par défaut, lancer le projet principal démarre le menu interactif dans le terminal :

```bash
dotnet run --project AG
```

### Fonctionnalités de la console :
- **1.** Afficher les tickets ouverts triés par priorité décroissante avec couleurs.
- **2.** Afficher tous les tickets avec statut `[OUVERT]` ou `[FERMÉ]`.
- **3.** Créer un nouveau ticket (validation anti-titre vide).
- **4.** Clôturer un ticket par ID.
- **5.** Supprimer un ticket par ID avec confirmation `(o/N)`.
- **0.** Quitter.

---

## 🌐 3. Lancer l'API REST (Minimal API)

Pour exposer les tickets sur le réseau via HTTP :

```bash
dotnet run --project AG -- --api
```

> Optionnellement, vous pouvez fixer le port d'écoute :
> ```bash
> dotnet run --project AG -- --api --urls "http://localhost:5000"
> ```

### Endpoints HTTP :

| Méthode | Route | Description | Exemple de réponse |
|---|---|---|---|
| **GET** | `/tickets` | Liste tous les tickets (`?openOnly=true` possible) | `200 OK` (Array) |
| **GET** | `/tickets/open` | Liste les tickets ouverts triés par priorité | `200 OK` (`{"totalOpen": 3, ...}`) |
| **GET** | `/tickets/{id}` | Détail d'un ticket par son ID | `200 OK` ou `404 Not Found` |
| **POST** | `/tickets` | Crée un nouveau ticket | `201 Created` |
| **PUT** | `/tickets/{id}/close` | Clôture un ticket | `200 OK` |
| **DELETE** | `/tickets/{id}` | Supprime un ticket | `204 No Content` |

#### Exemples de requêtes `curl` :
```bash
# Consulter les tickets ouverts
curl -s http://localhost:5000/tickets/open

# Créer un ticket
curl -s -X POST http://localhost:5000/tickets \
  -H "Content-Type: application/json" \
  -d '{"title": "Bug critique en prod", "priority": "HIGH"}'

# Clôturer le ticket #1
curl -s -X PUT http://localhost:5000/tickets/1/close

# Supprimer le ticket #2
curl -s -X DELETE http://localhost:5000/tickets/2
```

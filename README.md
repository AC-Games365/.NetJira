# 🎫 Mini Jira Studio (.NET 10 & SQLite)

Une application complète simulant un environnement **Jira Agile / Scrum** en C# / .NET 10 avec **persistance sur base SQLite**.

---

## ✨ Nouveautés majeures de l'interface Bureau (`AG.Desktop`)

L'interface bureau Avalonia UI a été considérablement enrichie :

### 1. 🔍 Barre d'outils & Filtres en temps réel
- **Recherche instantanée** : filtre par frappe au clavier sur le titre et la description.
- **Filtre par Type** : `BUG`, `STORY`, `TASK` ou `Tous`.
- **Filtre par Développeur / Assigné** : liste dynamique des membres de l'équipe (`Clément A.`, `Sophie D.`, `Thomas M.`, etc.).
- **Bouton « Effacer filtres »**.

### 2. 🎯 Gestion Scrum & Story Points
- Chaque ticket dispose d'une estimation en **Story Points** (1, 2, 3, 5, 8, 13 pts).
- L'en-tête calcule automatiquement la **vélocité / somme des points** du sprint visualisé.
- Bouton **🏁 Clôturer le Sprint** pour finaliser le sprint actif.

### 3. 👤 Avatar & Attribution d'équipe
- Chaque carte affiche l'avatar avec les initiales de l'assigné (`[C]`, `[S]`, `[T]`).
- Double badge visuel sur chaque ticket :
  - **Type de ticket** : 🐞 `BUG` (rouge pourpre), 📖 `STORY` (vert émeraude), 📝 `TASK` (indigo).
  - **Niveau d'urgence** : `HIGH`, `MEDIUM`, `LOW`.

### 4. 🗂️ Board Kanban 3 Colonnes
- **📋 À FAIRE (TO DO)** : bouton `▶ Démarrer` pour basculer en cours et sélecteur de Sprint.
- **⚡ EN COURS (IN PROGRESS)** : boutons `◀ To Do` ou `✔ Terminer`.
- **✅ TERMINÉ (DONE)** : bouton `◀ Réouvrir` et mise en retrait visuelle des tâches complétées.
- Suppression immédiate `✕` sur chaque carte.

---

## 🚀 Comment lancer l'application

### 💻 Interface Bureau (Avalonia UI XAML)
```bash
dotnet run --project AG.Desktop
```
*(Ou dans JetBrains Rider : sélectionnez **AG.Desktop** et cliquez sur ▶️ Run).*

### 🖥️ Console interactive
```bash
dotnet run --project AG
```

### 🌐 API REST (Minimal API)
```bash
dotnet run --project AG -- --api --urls "http://localhost:5000"
```

---

## 🗄️ Données & Base SQLite
- **Emplacement :** `~/.local/share/MiniJiraAG/tickets.db` (macOS/Linux) ou `%LOCALAPPDATA%\MiniJiraAG\tickets.db` (Windows).
- Les données sont automatiquement conservées entre chaque lancement.

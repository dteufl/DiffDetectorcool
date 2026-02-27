// ============================================================
//  GamesController.cs  –  Haupt-Controller der Anwendung
//
//  Verarbeitet alle HTTP-Anfragen rund um Spiele:
//  GET  /Games/Dashboard → Winrate-Dashboard anzeigen
//  GET  /Games           → Spielhistorie anzeigen
//  GET  /Games/Create    → Formular für neues Spiel anzeigen
//  POST /Games/Create    → Neues Spiel speichern
//  GET  /Games/Edit/5    → Bearbeitungsformular für Spiel #5
//  POST /Games/Edit      → Geändertes Spiel speichern
//  GET  /Games/Delete/5  → Löschbestätigung für Spiel #5
//  POST /Games/Delete    → Spiel #5 löschen
//
//  WICHTIG: Jeder eingeloggte User sieht NUR seine eigenen Spiele!
//  Alle DB-Abfragen filtern nach UserId (aus dem Login-Cookie).
// ============================================================

// Importiert ASP.NET MVC → Controller, IActionResult, etc.
using Microsoft.AspNetCore.Mvc;

// Importiert [Authorize] Attribut → schützt alle Methoden dieses Controllers
// Ein nicht eingeloggter User wird automatisch zu /Account/Login weitergeleitet
using Microsoft.AspNetCore.Authorization;

// Importiert Entity Framework Core → ToListAsync(), FindAsync(), etc.
using Microsoft.EntityFrameworkCore;

// Importiert Security-Klassen → ClaimTypes (um die UserId aus dem Cookie zu lesen)
using System.Security.Claims;

// Importiert unseren DbContext (Datenbankverbindung)
using DiffDetector.Data;

// Importiert unsere Model-Klassen
using DiffDetector.Models;

// Alle Klassen gehören zum Namespace DiffDetector.Controllers
namespace DiffDetector.Controllers
{
    // [Authorize] = ALLE Methoden dieses Controllers sind nur für eingeloggte User zugänglich!
    // Wenn ein nicht eingeloggter User /Games/Dashboard aufruft:
    //   → ASP.NET prüft das Cookie → kein Cookie vorhanden
    //   → Automatische Weiterleitung zu /Account/Login (konfiguriert in Program.cs)
    // Nach dem Login kommt der User automatisch zurück zur ursprünglichen Seite
    [Authorize]
    // Erbt von Controller → bekommt alle MVC-Funktionen (View, Redirect, etc.)
    public class GamesController : Controller
    {
        // _db = private Referenz auf den AppDbContext (Datenbankverbindung)
        // readonly = kann nach dem Konstruktor nicht mehr geändert werden
        // Konvention: private Felder beginnen mit Unterstrich _
        private readonly AppDbContext _db;

        // Konstruktor: ASP.NET injiziert den AppDbContext automatisch (Dependency Injection)
        // → wir müssen _db nie manuell erstellen, ASP.NET gibt es uns
        public GamesController(AppDbContext db)
        {
            // Speichert den injizierten DbContext in unserem privaten Feld
            _db = db;
        }

        // ── Hilfsmethode: UserId des eingeloggten Users lesen ─
        // Diese private Methode wird in jeder Action verwendet um den eingeloggten User zu identifizieren.
        //
        // Wie funktioniert das?
        // Beim Login speichert AccountController einen Claim im Cookie:
        //   new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
        // → Die Datenbank-Id des Users landet verschlüsselt im Browser-Cookie
        //
        // Hier lesen wir diesen Claim wieder aus:
        //   User             = HttpContext.User = der eingeloggte User (aus dem Cookie)
        //   FindFirstValue() = sucht den Claim mit dem angegebenen Typ
        //   int.Parse()      = wandelt den String "3" in die Zahl 3 um
        private int GetUserId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // ════════════════════════════════════════════════════
        //  DASHBOARD  –  Alle Statistiken berechnen und anzeigen
        //  URL: GET /Games/Dashboard
        // ════════════════════════════════════════════════════

        // async = die Methode läuft asynchron (wartet auf DB ohne den Thread zu blockieren)
        // Task<IActionResult> = asynchroner Rückgabetyp für Controller-Methoden
        public async Task<IActionResult> Dashboard()
        {
            // Liest die Id des eingeloggten Users aus dem Cookie
            var userId = GetUserId();

            // Holt die letzten 100 Spiele NUR dieses Users aus der Datenbank
            // Where(g => g.UserId == userId) = filtert nach dem eingeloggten User
            // → SQL: SELECT * FROM GameEntries WHERE UserId = 3 ORDER BY PlayedAt DESC LIMIT 100
            // Ohne diesen Filter würden alle User alle Spiele sehen!
            var games = await _db.GameEntries
                .Where(g => g.UserId == userId)
                .OrderByDescending(g => g.PlayedAt)
                .Take(100)
                .ToListAsync();

            // Wenn dieser User noch keine Spiele eingetragen hat:
            // Gibt ein leeres DashboardViewModel zurück → View zeigt "Noch keine Spiele"
            if (!games.Any())
                return View(new DashboardViewModel());

            // ── Champion-Statistiken berechnen (LINQ) ────────

            // GroupBy(g => g.Champion) = gruppiert alle Spiele nach Champion-Name
            // → z.B. alle "Jinx"-Spiele in einer Gruppe, alle "Zed"-Spiele in einer anderen
            var champStats = games
                .GroupBy(g => g.Champion)

                // Select = transformiert jede Gruppe in ein ChampionStats-Objekt
                .Select(grp => new ChampionStats
                {
                    // grp.Key = der Champion-Name (z.B. "Jinx")
                    Champion = grp.Key,

                    // grp.Count() = Anzahl Spiele in dieser Gruppe
                    Games = grp.Count(),

                    // Zählt nur die Spiele wo Won == true
                    Wins = grp.Count(g => g.Won),

                    // Durchschnittliche Kills aller Spiele dieser Gruppe
                    // Math.Round(..., 1) = auf 1 Dezimalstelle runden
                    AvgKills   = Math.Round(grp.Average(g => g.Kills),   1),
                    AvgDeaths  = Math.Round(grp.Average(g => g.Deaths),  1),
                    AvgAssists = Math.Round(grp.Average(g => g.Assists), 1)
                })

                // Sortiert nach meisten Spielen (häufigst gespielter Champion zuerst)
                .OrderByDescending(s => s.Games)
                .ToList(); // Konvertiert das Ergebnis in eine Liste

            // ── Matchup-Statistiken berechnen ────────────────

            // Gruppiert nach KOMBINATION aus eigenem Champion + Gegner
            // → z.B. "Jinx vs Caitlyn" als eigene Gruppe
            var matchupStats = games
                .GroupBy(g => new { g.Champion, g.EnemyChampion })
                .Select(grp => new MatchupStats
                {
                    Champion      = grp.Key.Champion,
                    EnemyChampion = grp.Key.EnemyChampion,
                    Games         = grp.Count(),
                    Wins          = grp.Count(g => g.Won)
                })
                // Nur Matchups mit mindestens 2 Spielen anzeigen
                .Where(m => m.Games >= 2)
                .ToList();

            // ── Lane-Statistiken berechnen ────────────────────

            // Gruppiert alle Spiele nach Lane (Top, Mid, etc.)
            var laneStats = games
                .GroupBy(g => g.Lane)
                .Select(grp => new LaneStats
                {
                    Lane  = grp.Key,
                    Games = grp.Count(),
                    Wins  = grp.Count(g => g.Won)
                })
                .OrderByDescending(l => l.Games)
                .ToList();

            // ── Win/Loss-Streak berechnen ─────────────────────

            // Startet den Streak-Zähler bei 0
            int streak = 0;

            // Schaut ob das neueste Spiel gewonnen oder verloren wurde
            bool streakIsWin = games.First().Won;

            // Geht durch die Spiele (neuestes zuerst)
            foreach (var g in games)
            {
                // Solange das Ergebnis gleich wie der Streak-Typ ist: zählen
                if (g.Won == streakIsWin) streak++;
                // Sobald das Ergebnis anders ist: Streak ist vorbei → abbrechen
                else break;
            }

            // ── Bester Champion ───────────────────────────────

            // Sucht den Champion mit der höchsten Winrate (mind. 3 Spiele)
            var bestChamp = champStats
                .Where(c => c.Games >= 3)
                .OrderByDescending(c => c.WinRate)
                .FirstOrDefault();

            // ── ViewModel zusammenbauen und an View übergeben ─
            var vm = new DashboardViewModel
            {
                ChampionStats      = champStats,
                BestMatchups       = matchupStats.OrderByDescending(m => m.WinRate).Take(5).ToList(),
                WorstMatchups      = matchupStats.OrderBy(m => m.WinRate).Take(5).ToList(),
                LaneStats          = laneStats,
                TotalGames         = games.Count,
                OverallWinRate     = Math.Round((double)games.Count(g => g.Won) / games.Count * 100, 1),
                MostPlayedChampion = champStats.FirstOrDefault()?.Champion ?? "-",
                BestChampion       = bestChamp?.Champion ?? "-",
                RecentGames        = games.Take(10).ToList(),
                CurrentStreak      = streak,
                StreakIsWin        = streakIsWin
            };

            // Übergibt das ViewModel an Dashboard.cshtml → View rendert das HTML
            return View(vm);
        }

        // ════════════════════════════════════════════════════
        //  INDEX  –  Spielhistorie mit Filter anzeigen
        //  URL: GET /Games  oder  GET /Games?champion=Jinx&lane=Mid
        // ════════════════════════════════════════════════════

        public async Task<IActionResult> Index(string? champion, string? lane, string? result)
        {
            // Liest die Id des eingeloggten Users
            var userId = GetUserId();

            // Startet die Abfrage, filtert sofort nach UserId
            // → nur Spiele des eingeloggten Users werden berücksichtigt
            var query = _db.GameEntries
                .Where(g => g.UserId == userId)
                .AsQueryable();

            // Champion-Filter: wenn ein Champion-Name eingegeben wurde
            // ToLower() = Groß-/Kleinschreibung ignorieren (jinx == Jinx == JINX)
            // Contains() = sucht ob der Name im Champion-Feld vorkommt (Teilsuche)
            if (!string.IsNullOrWhiteSpace(champion))
                query = query.Where(g => g.Champion.ToLower().Contains(champion.ToLower()));

            // Lane-Filter: exakte Übereinstimmung (z.B. "Mid")
            if (!string.IsNullOrWhiteSpace(lane))
                query = query.Where(g => g.Lane == lane);

            // Ergebnis-Filter: nur Siege oder nur Niederlagen
            if (result == "win")
                query = query.Where(g => g.Won);        // Won == true
            else if (result == "loss")
                query = query.Where(g => !g.Won);       // Won == false

            // Datenbankabfrage ausführen mit allen Filtern
            var games = await query
                .OrderByDescending(g => g.PlayedAt)
                .Take(100)
                .ToListAsync();

            // ViewBag = dynamisches Objekt zum Übergeben von Werten an die View
            // → View kann damit die Filter-Felder wieder vorausfüllen
            ViewBag.FilterChampion = champion;
            ViewBag.FilterLane     = lane;
            ViewBag.FilterResult   = result;

            return View(games);
        }

        // ════════════════════════════════════════════════════
        //  CREATE GET  –  Leeres Formular anzeigen
        //  URL: GET /Games/Create
        // ════════════════════════════════════════════════════

        public IActionResult Create() => View();

        // ════════════════════════════════════════════════════
        //  CREATE POST  –  Formular abschicken, Spiel speichern
        //  URL: POST /Games/Create
        // ════════════════════════════════════════════════════

        // [HttpPost] = diese Methode reagiert nur auf POST-Requests (Formular abschicken)
        [HttpPost]
        // [ValidateAntiForgeryToken] = Sicherheitscheck gegen CSRF-Angriffe
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(GameEntry entry)
        {
            // ModelState.IsValid prüft alle DataAnnotations aus GameEntry.cs
            if (!ModelState.IsValid)
                return View(entry);

            // UserId auf den eingeloggten User setzen
            // → dieses Spiel gehört ab jetzt diesem User
            // WICHTIG: das darf NICHT aus dem Formular kommen (Sicherheit!)
            // → ein böswilliger User könnte sonst eine fremde UserId schicken
            // → deshalb setzen WIR die UserId hier im Controller, nicht im Formular
            entry.UserId   = GetUserId();
            entry.PlayedAt = DateTime.UtcNow;

            // INSERT INTO GameEntries (..., UserId) VALUES (..., 3)
            _db.GameEntries.Add(entry);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"{(entry.Won ? "WIN ✓" : "LOSS")} mit {entry.Champion} eingetragen!";
            return RedirectToAction(nameof(Dashboard));
        }

        // ════════════════════════════════════════════════════
        //  EDIT GET  –  Bearbeitungsformular anzeigen
        //  URL: GET /Games/Edit/5
        // ════════════════════════════════════════════════════

        public async Task<IActionResult> Edit(int id)
        {
            var userId = GetUserId();

            // Sucht den Eintrag anhand Id UND UserId
            // → ein User kann nur SEINE eigenen Einträge bearbeiten!
            // Ohne UserId-Check könnte User A den Eintrag von User B bearbeiten
            // indem er einfach /Games/Edit/99 aufruft
            var entry = await _db.GameEntries
                .FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);

            // Wenn Id nicht existiert ODER dem falschen User gehört → 404
            if (entry == null) return NotFound();

            return View(entry);
        }

        // ════════════════════════════════════════════════════
        //  EDIT POST  –  Geänderte Daten speichern
        //  URL: POST /Games/Edit
        // ════════════════════════════════════════════════════

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(GameEntry entry)
        {
            if (!ModelState.IsValid)
                return View(entry);

            // Sicherheitscheck: Gehört dieser Eintrag wirklich dem eingeloggten User?
            // Nochmal aus DB laden um sicherzustellen dass die UserId stimmt
            var userId = GetUserId();
            var exists = await _db.GameEntries
                .AnyAsync(g => g.Id == entry.Id && g.UserId == userId);

            // Wenn der Eintrag nicht diesem User gehört → 403 Forbidden
            if (!exists) return Forbid();

            // UserId explizit setzen damit sie nicht aus dem Formular überschrieben werden kann
            entry.UserId = userId;

            // UPDATE GameEntries SET ... WHERE Id = ...
            _db.Update(entry);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // ════════════════════════════════════════════════════
        //  DELETE GET  –  Löschbestätigung anzeigen
        //  URL: GET /Games/Delete/5
        // ════════════════════════════════════════════════════

        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetUserId();

            // Eintrag nur laden wenn er auch diesem User gehört
            var entry = await _db.GameEntries
                .FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);

            if (entry == null) return NotFound();

            return View(entry);
        }

        // ════════════════════════════════════════════════════
        //  DELETE POST  –  Spiel wirklich löschen
        //  URL: POST /Games/Delete
        // ════════════════════════════════════════════════════

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = GetUserId();

            // Nur löschen wenn der Eintrag diesem User gehört
            // → verhindert dass User A den Eintrag von User B löscht
            var entry = await _db.GameEntries
                .FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);

            if (entry != null)
            {
                // DELETE FROM GameEntries WHERE Id = ... AND UserId = ...
                _db.GameEntries.Remove(entry);
                await _db.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}

// ============================================================
//  StatsViewModels.cs  –  ViewModels für das Dashboard
//  ViewModels sind reine "Daten-Container" die vom Controller
//  berechnet und dann an die View (Dashboard.cshtml) übergeben werden.
//  Sie existieren NUR in C#, haben keine eigene Datenbanktabelle.
// ============================================================

// Alle Klassen gehören zum Namespace DiffDetector.Models
namespace DiffDetector.Models
{
    // ── ChampionStats ────────────────────────────────────────
    // Enthält die berechneten Statistiken für EINEN Champion
    // Wird für jede Champion-Gruppe aus den Spielen berechnet
    public class ChampionStats
    {
        // Name des Champions (z.B. "Jinx", "Yasuo")
        public string Champion { get; set; } = string.Empty;

        // Gesamtanzahl der gespielten Spiele mit diesem Champion
        public int Games { get; set; }

        // Anzahl der gewonnenen Spiele
        public int Wins { get; set; }

        // Anzahl der verlorenen Spiele
        // => (Pfeil-Property) = wird automatisch aus Games und Wins berechnet
        // kein eigenes Feld nötig, wird immer frisch berechnet
        public int Losses => Games - Wins;

        // Winrate in Prozent, auf 1 Nachkommastelle gerundet
        // Prüft zuerst ob Games == 0 (kein Spiel → keine Division durch 0!)
        // Math.Round(..., 1) rundet auf 1 Dezimalstelle (z.B. 66.666... → 66.7)
        public double WinRate => Games == 0 ? 0 : Math.Round((double)Wins / Games * 100, 1);

        // Durchschnittliche Kills pro Spiel (wird vom Controller gesetzt)
        public double AvgKills { get; set; }

        // Durchschnittliche Tode pro Spiel
        public double AvgDeaths { get; set; }

        // Durchschnittliche Assists pro Spiel
        public double AvgAssists { get; set; }

        // KDA-Wert = (Kills + Assists) / Deaths
        // Wenn AvgDeaths == 0 → "Perfect" (kein Tod = perfektes KDA)
        // Math.Round(..., 2) = auf 2 Nachkommastellen runden
        // :0.00 = Formatierung → immer 2 Dezimalstellen (z.B. 3.50 statt 3.5)
        public string KDA => AvgDeaths == 0
            ? "Perfect"
            : $"{Math.Round((AvgKills + AvgAssists) / AvgDeaths, 2):0.00}";
    }

    // ── MatchupStats ─────────────────────────────────────────
    // Statistiken für ein bestimmtes Champion-vs-Champion Matchup
    // z.B. "Jinx gegen Caitlyn: 3 Spiele, 2 Siege → 66.7% WR"
    public class MatchupStats
    {
        // Eigener Champion
        public string Champion { get; set; } = string.Empty;

        // Gegnerischer Champion
        public string EnemyChampion { get; set; } = string.Empty;

        // Anzahl der Spiele in genau diesem Matchup
        public int Games { get; set; }

        // Anzahl der Siege in diesem Matchup
        public int Wins { get; set; }

        // Berechnet Niederlagen automatisch
        public int Losses => Games - Wins;

        // Winrate für dieses spezifische Matchup
        public double WinRate => Games == 0 ? 0 : Math.Round((double)Wins / Games * 100, 1);
    }

    // ── LaneStats ────────────────────────────────────────────
    // Statistiken pro Lane (Top, Jungle, Mid, Bot/ADC, Support)
    public class LaneStats
    {
        // Name der Lane (z.B. "Mid", "Support")
        public string Lane { get; set; } = string.Empty;

        // Anzahl Spiele auf dieser Lane
        public int Games { get; set; }

        // Anzahl Siege auf dieser Lane
        public int Wins { get; set; }

        // Winrate auf dieser Lane
        public double WinRate => Games == 0 ? 0 : Math.Round((double)Wins / Games * 100, 1);
    }

    // ── DashboardViewModel ───────────────────────────────────
    // Das Haupt-ViewModel für das Dashboard
    // Wird vom GamesController.Dashboard() befüllt und an Dashboard.cshtml übergeben
    // Fasst ALLE Statistiken in einem Objekt zusammen
    public class DashboardViewModel
    {
        // Liste aller Champion-Statistiken (eine pro gespieltem Champion)
        public List<ChampionStats> ChampionStats { get; set; } = new();

        // Die 5 besten Matchups (höchste Winrate, min. 2 Spiele)
        public List<MatchupStats> BestMatchups { get; set; } = new();

        // Die 5 schlechtesten Matchups (niedrigste Winrate, min. 2 Spiele)
        public List<MatchupStats> WorstMatchups { get; set; } = new();

        // Winrate pro Lane
        public List<LaneStats> LaneStats { get; set; } = new();

        // Gesamtanzahl der analysierten Spiele (max. 100)
        public int TotalGames { get; set; }

        // Gesamt-Winrate über alle Spiele
        public double OverallWinRate { get; set; }

        // Champion mit den meisten Spielen
        public string MostPlayedChampion { get; set; } = "-";

        // Champion mit der höchsten Winrate (mind. 3 Spiele)
        public string BestChampion { get; set; } = "-";

        // Die 10 zuletzt eingetragenen Spiele für die "Letzte Spiele" Tabelle
        public List<GameEntry> RecentGames { get; set; } = new();

        // Aktuelle Win- oder Loss-Streak (z.B. 3 Siege hintereinander)
        public int CurrentStreak { get; set; }

        // true = aktueller Streak ist eine Win-Streak, false = Loss-Streak
        public bool StreakIsWin { get; set; }
    }
}

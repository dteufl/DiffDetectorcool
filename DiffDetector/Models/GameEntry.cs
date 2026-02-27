// ============================================================
//  GameEntry.cs  –  Das Datenmodell (Model in MVC)
//  Diese Klasse repräsentiert eine gespielte League of Legends Runde.
//  EF Core liest diese Klasse und erstellt daraus automatisch
//  die Tabelle "GameEntries" in der SQLite-Datenbank.
//  Jede Property = eine Spalte in der Tabelle.
// ============================================================

// Importiert DataAnnotations → ermöglicht Validierungs-Attribute wie [Required]
using System.ComponentModel.DataAnnotations;

// Alle Klassen in diesem File gehören zum Namespace DiffDetector.Models
namespace DiffDetector.Models
{
    // public class = öffentliche Klasse, von überall im Projekt verwendbar
    // GameEntry = Name der Klasse UND (durch EF Core Konvention) Basis für den Tabellennamen
    // EF Core benennt die Tabelle automatisch "GameEntries" (Plural)
    public class GameEntry
    {
        // Id = Primärschlüssel der Tabelle (EF Core erkennt "Id" automatisch als PK)
        // int = ganzzahliger Typ, EF Core setzt AUTOINCREMENT → jeder Eintrag bekommt eine eindeutige Zahl
        public int Id { get; set; }

        // ── BESITZER DES EINTRAGS ────────────────────────────

        // UserId = Fremdschlüssel → verknüpft diesen Eintrag mit einem bestimmten User
        // Jedes Spiel gehört genau einem User (z.B. UserId = 3 → gehört User mit Id=3)
        // Beim Speichern setzt der Controller diese Id auf den eingeloggten User
        // Beim Lesen filtert der Controller: WHERE UserId = <eingeloggter User>
        // → So sieht jeder User NUR seine eigenen Spiele!
        public int UserId { get; set; }

        // ── EIGENE SEITE ────────────────────────────────────

        // [Required] = Pflichtfeld, darf nicht leer sein
        // → MVC zeigt automatisch eine Fehlermeldung wenn das Feld leer ist
        [Required(ErrorMessage = "Bitte deinen Champion eingeben.")]

        // [StringLength(60)] = maximale Länge 60 Zeichen
        // → verhindert zu lange Eingaben, begrenzt die Spaltenbreite in der DB
        [StringLength(60)]

        // [Display(Name = ...)] = Anzeigename im Formular (statt "Champion" → "Dein Champion")
        [Display(Name = "Dein Champion")]

        // string = Textfeld
        // = string.Empty verhindert null-Warnungen (Nullable ist aktiviert)
        public string Champion { get; set; } = string.Empty;

        // Pflichtfeld: Lane muss ausgewählt werden
        [Required(ErrorMessage = "Bitte eine Lane wählen.")]
        [Display(Name = "Lane")]
        public string Lane { get; set; } = string.Empty;

        // Pflichtfeld: Gegner-Champion muss angegeben werden
        [Required(ErrorMessage = "Bitte den Gegner-Champion eingeben.")]
        [StringLength(60)]
        [Display(Name = "Gegner-Champion")]
        public string EnemyChampion { get; set; } = string.Empty;

        // ── ERGEBNIS ────────────────────────────────────────

        // bool = true (Sieg) oder false (Niederlage)
        // In SQLite wird bool als 0 (false) oder 1 (true) gespeichert
        [Display(Name = "Ergebnis")]
        public bool Won { get; set; }

        // ── OPTIONAL ────────────────────────────────────────

        // Spielmodus, Standard-Wert "Ranked Solo/Duo" wird vorausgefüllt
        [StringLength(30)]
        [Display(Name = "Game-Modus")]
        public string GameMode { get; set; } = "Ranked Solo/Duo";

        // Datum + Uhrzeit wann das Spiel eingetragen wurde
        // DateTime.UtcNow = aktuelle Zeit in UTC (Weltzeit, unabhängig von Zeitzone)
        [Display(Name = "Datum")]
        public DateTime PlayedAt { get; set; } = DateTime.UtcNow;

        // Optionale Notizen (? nach string = darf null sein, also leer bleiben)
        [StringLength(500)]
        [Display(Name = "Notizen")]
        public string? Notes { get; set; }

        // ── KDA (Kills / Deaths / Assists) ──────────────────

        // [Range(0, 50)] = Wert muss zwischen 0 und 50 liegen → Validierung
        [Range(0, 50)]
        public int Kills { get; set; }    // Anzahl Kills

        [Range(0, 50)]
        public int Deaths { get; set; }   // Anzahl Tode

        [Range(0, 50)]
        public int Assists { get; set; }  // Anzahl Assists
    }
}

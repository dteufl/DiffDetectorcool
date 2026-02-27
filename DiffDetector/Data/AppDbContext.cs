// ============================================================
//  AppDbContext.cs  –  Datenbankverbindung (EF Core)
//  Der DbContext ist die Brücke zwischen C#-Objekten und der SQLite-Datenbank.
//  EF Core übersetzt C#-Befehle automatisch in SQL (SELECT, INSERT, UPDATE, DELETE).
// ============================================================

// Importiert Entity Framework Core → brauchen wir für DbContext und DbSet
using Microsoft.EntityFrameworkCore;

// Importiert unsere Model-Klassen (GameEntry, User)
using DiffDetector.Models;

// Alle Klassen in diesem File gehören zum Namespace DiffDetector.Data
namespace DiffDetector.Data
{
    // AppDbContext erbt von DbContext (EF Core Basisklasse)
    // → dadurch bekommt er alle Datenbankfunktionen automatisch
    public class AppDbContext : DbContext
    {
        // Konstruktor: empfängt die Konfiguration per Dependency Injection
        // DbContextOptions enthält z.B. den Connection String (Pfad zur .db Datei)
        // : base(options) gibt die Konfiguration an die EF Core Basisklasse weiter
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // DbSet<GameEntry> repräsentiert die Tabelle "GameEntries" in der Datenbank
        // Jede Zeile in der Tabelle = ein GameEntry-Objekt in C#
        // Über GameEntries können wir CRUD-Operationen auf die Tabelle machen:
        //   _db.GameEntries.ToListAsync()    → SELECT * FROM GameEntries
        //   _db.GameEntries.Add(entry)       → INSERT INTO GameEntries ...
        //   _db.GameEntries.Remove(entry)    → DELETE FROM GameEntries ...
        public DbSet<GameEntry> GameEntries => Set<GameEntry>();

        // DbSet<User> repräsentiert die Tabelle "Users" in der Datenbank
        // Jede Zeile = ein registrierter Benutzer (Username + Passwort-Hash)
        // Wird vom AccountController für Login und Registrierung verwendet
        public DbSet<User> Users => Set<User>();
    }
}

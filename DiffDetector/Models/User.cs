// ============================================================
//  User.cs  –  Datenmodell für einen registrierten Benutzer
//  EF Core erstellt daraus die Tabelle "Users" in der SQLite-DB.
//  WICHTIG: Das Passwort wird NIE im Klartext gespeichert,
//  sondern immer als BCrypt-Hash (z.B. "$2a$11$xyz...").
// ============================================================

// Importiert DataAnnotations für Validierungs-Attribute
using System.ComponentModel.DataAnnotations;

// Alle Klassen gehören zum Namespace DiffDetector.Models
namespace DiffDetector.Models
{
    // User-Klasse = eine Zeile in der Tabelle "Users"
    public class User
    {
        // Primärschlüssel, wird von EF Core automatisch als AUTOINCREMENT gesetzt
        public int Id { get; set; }

        // Benutzername: Pflichtfeld, max. 50 Zeichen
        // wird beim Login und in der Navbar angezeigt
        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        // Passwort-Hash: wird NIEMALS als Klartext gespeichert!
        // BCrypt.HashPassword("meinPasswort") → "$2a$11$randomSalt+Hash..."
        // Der Hash sieht immer anders aus, auch bei gleichem Passwort (Salt)
        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        // Zeitpunkt der Registrierung (wird automatisch gesetzt)
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

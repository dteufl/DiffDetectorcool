// ============================================================
//  AccountViewModels.cs  –  ViewModels für Login und Registrierung
//  Diese Klassen sind NUR für die Formulare zuständig.
//  Sie haben keine eigene Datenbanktabelle.
//  Die Daten werden vom Controller geprüft und dann in
//  das User-Model übertragen.
// ============================================================

using System.ComponentModel.DataAnnotations;

namespace DiffDetector.Models
{
    // ── LoginViewModel ───────────────────────────────────────
    // Wird vom Login-Formular (Login.cshtml) verwendet.
    // Enthält nur die Felder die der User beim Login eingibt.
    public class LoginViewModel
    {
        // Benutzername: Pflichtfeld
        [Required(ErrorMessage = "Bitte einen Benutzernamen eingeben.")]
        [Display(Name = "Benutzername")]
        public string Username { get; set; } = string.Empty;

        // Passwort: Pflichtfeld
        // [DataType(Password)] sagt der View: dieses Feld als Passwort-Input rendern
        // → Browser zeigt Punkte/Sterne statt den echten Buchstaben
        [Required(ErrorMessage = "Bitte ein Passwort eingeben.")]
        [DataType(DataType.Password)]
        [Display(Name = "Passwort")]
        public string Password { get; set; } = string.Empty;
    }

    // ── RegisterViewModel ────────────────────────────────────
    // Wird vom Registrierungs-Formular (Register.cshtml) verwendet.
    // Hat ein extra "Passwort bestätigen" Feld zur Sicherheit.
    public class RegisterViewModel
    {
        // Benutzername: Pflichtfeld, 3–50 Zeichen
        [Required(ErrorMessage = "Bitte einen Benutzernamen eingeben.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Benutzername muss 3–50 Zeichen lang sein.")]
        [Display(Name = "Benutzername")]
        public string Username { get; set; } = string.Empty;

        // Passwort: Pflichtfeld, mindestens 6 Zeichen
        [Required(ErrorMessage = "Bitte ein Passwort eingeben.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Passwort muss mindestens 6 Zeichen lang sein.")]
        [DataType(DataType.Password)]
        [Display(Name = "Passwort")]
        public string Password { get; set; } = string.Empty;

        // Passwort-Bestätigung: muss identisch mit Password sein
        // [Compare("Password")] = Validierung: dieses Feld muss gleich wie "Password" sein
        // → verhindert Tippfehler beim Passwort setzen
        [Required(ErrorMessage = "Bitte das Passwort bestätigen.")]
        [Compare("Password", ErrorMessage = "Die Passwörter stimmen nicht überein.")]
        [DataType(DataType.Password)]
        [Display(Name = "Passwort bestätigen")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}

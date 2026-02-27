// ============================================================
//  AccountController.cs  –  Login, Registrierung, Logout
//
//  Verarbeitet alle Authentifizierungs-Anfragen:
//  GET  /Account/Login     → Login-Formular anzeigen
//  POST /Account/Login     → Login prüfen, Cookie setzen
//  GET  /Account/Register  → Registrierungs-Formular anzeigen
//  POST /Account/Register  → Neuen User anlegen
//  POST /Account/Logout    → Cookie löschen, ausloggen
// ============================================================

// Importiert ASP.NET MVC → Controller, IActionResult, etc.
using Microsoft.AspNetCore.Mvc;

// Importiert die Security-Klassen für Cookie-Authentifizierung
using System.Security.Claims;

// Importiert AuthenticationProperties und CookieAuthenticationDefaults
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

// Importiert Entity Framework Core → FirstOrDefaultAsync, AnyAsync, etc.
using Microsoft.EntityFrameworkCore;

// Importiert unseren DbContext und Models
using DiffDetector.Data;
using DiffDetector.Models;

namespace DiffDetector.Controllers
{
    public class AccountController : Controller
    {
        // _db = Datenbankverbindung, per Dependency Injection injiziert
        private readonly AppDbContext _db;

        // Konstruktor: ASP.NET gibt uns den AppDbContext automatisch
        public AccountController(AppDbContext db)
        {
            _db = db;
        }

        // ════════════════════════════════════════════════════
        //  LOGIN GET  –  Login-Formular anzeigen
        //  URL: GET /Account/Login
        // ════════════════════════════════════════════════════

        public IActionResult Login()
        {
            // Wenn der User bereits eingeloggt ist → direkt zum Dashboard
            // User.Identity.IsAuthenticated = true wenn ein gültiges Cookie vorhanden ist
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Dashboard", "Games");

            // Leeres Login-Formular anzeigen
            return View();
        }

        // ════════════════════════════════════════════════════
        //  LOGIN POST  –  Zugangsdaten prüfen, einloggen
        //  URL: POST /Account/Login
        // ════════════════════════════════════════════════════

        [HttpPost]
        // [ValidateAntiForgeryToken] schützt gegen CSRF-Angriffe
        // (bösweilige Seite könnte sonst Login-Requests in deinem Namen schicken)
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            // Prüft [Required] Validierungen aus LoginViewModel
            if (!ModelState.IsValid)
                return View(model);

            // Sucht den User in der DB anhand des Benutzernamens
            // FirstOrDefaultAsync = gibt ersten Treffer zurück, oder null wenn nicht gefunden
            // StringComparison.OrdinalIgnoreCase = Groß-/Kleinschreibung ignorieren
            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == model.Username.ToLower());

            // Prüft ob User existiert UND ob das Passwort stimmt
            // BCrypt.Verify(eingabe, hash) = vergleicht Klartext-Passwort mit dem gespeicherten Hash
            // → gibt true zurück wenn das Passwort korrekt ist
            // WICHTIG: wir können den Hash nicht "zurückrechnen" → nur vergleichen
            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                // Bewusst vage Fehlermeldung: sagt nicht ob Username oder Passwort falsch ist
                // → Angreifer soll nicht wissen ob ein Username existiert
                ModelState.AddModelError("", "Benutzername oder Passwort falsch.");
                return View(model);
            }

            // ── Login-Cookie erstellen ────────────────────────

            // Claims = Informationen über den eingeloggten User die im Cookie gespeichert werden
            // Claim = ein "Behauptung" über den User (z.B. "sein Name ist Max")
            var claims = new List<Claim>
            {
                // NameIdentifier = eindeutige ID des Users (die Datenbank-Id)
                // wird verwendet um den User bei jedem Request zu identifizieren
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),

                // Name = Benutzername, wird in der Navbar angezeigt (User.Identity.Name)
                new Claim(ClaimTypes.Name, user.Username)
            };

            // ClaimsIdentity = Identität des Users, enthält alle Claims
            // "CookieAuth" = Name des Authentifizierungsschemas (muss mit Program.cs übereinstimmen)
            var identity = new ClaimsIdentity(claims, "CookieAuth");

            // ClaimsPrincipal = der "Sicherheitsprinzipal" (der eingeloggte User)
            // Kann mehrere Identitäten haben (z.B. Windows + Cookie)
            var principal = new ClaimsPrincipal(identity);

            // HttpContext.SignInAsync = erstellt das verschlüsselte Cookie und schickt es an den Browser
            // Ab jetzt ist der User eingeloggt → bei jedem Request wird das Cookie geprüft
            await HttpContext.SignInAsync("CookieAuth", principal);

            // Weiterleitung zum Dashboard nach erfolgreichem Login
            return RedirectToAction("Dashboard", "Games");
        }

        // ════════════════════════════════════════════════════
        //  REGISTER GET  –  Registrierungs-Formular anzeigen
        //  URL: GET /Account/Register
        // ════════════════════════════════════════════════════

        public IActionResult Register()
        {
            // Wenn bereits eingeloggt → zum Dashboard
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Dashboard", "Games");

            // Leeres Registrierungs-Formular anzeigen
            return View();
        }

        // ════════════════════════════════════════════════════
        //  REGISTER POST  –  Neuen User anlegen
        //  URL: POST /Account/Register
        // ════════════════════════════════════════════════════

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            // Validierungen prüfen: [Required], [StringLength], [Compare]
            if (!ModelState.IsValid)
                return View(model);

            // Prüft ob der Benutzername bereits vergeben ist
            // AnyAsync = gibt true zurück wenn mindestens ein Eintrag gefunden wurde
            // → SELECT COUNT(*) FROM Users WHERE Username = '...' > 0
            var usernameExists = await _db.Users
                .AnyAsync(u => u.Username.ToLower() == model.Username.ToLower());

            if (usernameExists)
            {
                // Fehlermeldung zum ModelState hinzufügen
                // "" = kein spezifisches Feld → wird in der Validation Summary angezeigt
                ModelState.AddModelError("", "Dieser Benutzername ist bereits vergeben.");
                return View(model);
            }

            // ── Neuen User erstellen ──────────────────────────

            // Passwort NIEMALS im Klartext speichern!
            // BCrypt.HashPassword() erstellt einen sicheren Hash:
            //   1. Generiert einen zufälligen "Salt" (z.B. "$2a$11$randomBytes")
            //   2. Kombiniert Salt + Passwort und hasht es mehrfach
            //   3. Ergebnis: "$2a$11$salt+hash" (z.B. 60 Zeichen langer String)
            // Selbst wenn die DB gestohlen wird, kann niemand die Passwörter lesen
            var newUser = new User
            {
                Username     = model.Username,
                // workFactor=11 = Rechenaufwand (höher = sicherer aber langsamer)
                // Standard ist 11, braucht ca. 100ms → macht Brute-Force schwieriger
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password, workFactor: 11),
                CreatedAt    = DateTime.UtcNow
            };

            // User zur Datenbank hinzufügen
            _db.Users.Add(newUser);

            // INSERT INTO Users (...) VALUES (...) ausführen
            await _db.SaveChangesAsync();

            // Erfolgsmeldung setzen (wird nach Weiterleitung angezeigt)
            TempData["Success"] = $"Account '{newUser.Username}' erstellt! Bitte einloggen.";

            // Nach der Registrierung → Login-Seite (User muss sich noch einloggen)
            return RedirectToAction(nameof(Login));
        }

        // ════════════════════════════════════════════════════
        //  LOGOUT POST  –  Ausloggen, Cookie löschen
        //  URL: POST /Account/Logout
        //  Nur POST (kein GET) um CSRF-Angriffe zu verhindern
        //  (bösweilige Links könnten sonst den User ausloggen)
        // ════════════════════════════════════════════════════

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            // SignOutAsync = löscht das Auth-Cookie aus dem Browser
            // → User ist danach ausgeloggt, [Authorize]-Seiten sind wieder gesperrt
            await HttpContext.SignOutAsync("CookieAuth");

            // Weiterleitung zur Login-Seite nach dem Logout
            return RedirectToAction(nameof(Login));
        }
    }
}

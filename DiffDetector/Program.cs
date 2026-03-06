// ============================================================
//  Program.cs  –  Einstiegspunkt der gesamten Anwendung
//  Hier wird die App konfiguriert und gestartet.
// ============================================================

// Importiert Entity Framework Core → wird für die Datenbankverbindung gebraucht
using Microsoft.EntityFrameworkCore;

// Importiert unseren eigenen DbContext (Datenbankverbindung)a
using DiffDetector.Data;

// Erstellt den "Builder" – sammelt alle Einstellungen bevor die App startet
var builder = WebApplication.CreateBuilder(args);

// Registriert MVC mit Views (Model-View-Controller Pattern)
// → ermöglicht Controller-Klassen und .cshtml Views zu verwenden
builder.Services.AddControllersWithViews();

// ── Cookie-Authentifizierung einrichten ───────────────────
// ASP.NET speichert den Login-Status in einem verschlüsselten Cookie im Browser.
// Wenn ein User eingeloggt ist, schickt der Browser bei jedem Request das Cookie mit.
// ASP.NET prüft das Cookie und weiß dadurch wer der User ist.
builder.Services.AddAuthentication("CookieAuth")

    // AddCookie = verwendet Cookies als Authentifizierungsmethode
    .AddCookie("CookieAuth", options =>
    {
        // LoginPath = wohin der User weitergeleitet wird wenn er nicht eingeloggt ist
        // → [Authorize] auf einem Controller schickt den User hierher
        options.LoginPath = "/Account/Login";

        // AccessDeniedPath = wohin der User kommt wenn er keine Berechtigung hat
        options.AccessDeniedPath = "/Account/Login";

        // ExpireTimeSpan = wie lange das Cookie (= der Login) gültig ist
        // TimeSpan.FromDays(7) = Cookie läuft nach 7 Tagen ab → User wird ausgeloggt
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
    });

// ── Datenbankpfad festlegen ────────────────────────────────
// AppContext.BaseDirectory = Ordner wo die .dll liegt
// Im Docker-Container: /app/     Lokal in VS: bin/Debug/net8.0/
// Path.Combine hängt "data" an diesen Pfad dran → ergibt z.B. /app/data
var dbFolder = Path.Combine(AppContext.BaseDirectory, "data");

// Erstellt den "data"-Ordner falls er noch nicht existiert
// Wichtig: SQLite kann die .db-Datei nicht anlegen wenn der Ordner fehlt
Directory.CreateDirectory(dbFolder);

// Vollständiger Pfad zur SQLite-Datenbankdatei
// Ergebnis z.B.: /app/data/diffdetector.db
var dbPath = Path.Combine(dbFolder, "diffdetector.db");

// Registriert den AppDbContext als Service (Dependency Injection)
// → ASP.NET erstellt automatisch eine DB-Verbindung wenn ein Controller sie braucht
// UseSqlite(...) sagt EF Core: "Benutze SQLite mit dieser Datei"
// $"Data Source={dbPath}" ist der Connection String (Verbindungszeichenkette)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// Baut die fertige App aus allen registrierten Services zusammen
var app = builder.Build();

// ── Datenbank beim Start automatisch erstellen ─────────────

// CreateScope() erstellt einen temporären Bereich um Services zu verwenden
// → nötig weil AppDbContext ein "Scoped Service" ist (lebt nur pro Request)
using (var scope = app.Services.CreateScope())
{
    // Holt den AppDbContext aus dem Dependency Injection Container
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // EnsureCreated() prüft: existiert die Datenbank schon?
    // NEIN → erstellt diffdetector.db + alle Tabellen automatisch aus den Models
    //        (GameEntries-Tabelle mit UserId-Spalte + Users-Tabelle werden erstellt)
    // JA   → macht nichts, bestehende Daten bleiben erhalten
    //
    // ACHTUNG beim Upgrade: Wenn du eine neue Spalte (z.B. UserId) hinzufügst,
    // muss die alte diffdetector.db gelöscht werden damit EnsureCreated
    // die Tabelle neu erstellt. Einfach den Docker-Container + Volume löschen:
    //   docker compose down -v   (löscht Container UND Volume mit der DB)
    //   docker compose up --build
    db.Database.EnsureCreated();
}

// ── Middleware Pipeline konfigurieren ─────────────────────
// Middleware = Verarbeitungsschritte die jeder HTTP-Request durchläuft
// REIHENFOLGE ist wichtig! Authentication muss vor Authorization kommen.

// Nur im Produktionsmodus: leitet Fehler auf /Home/Error weiter
// Im Development-Modus sieht man den kompletten Fehler + Stack Trace
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

// Ermöglicht statische Dateien aus wwwroot/ auszuliefern (CSS, JS, Bilder)
app.UseStaticFiles();

// Aktiviert das URL-Routing → bestimmt welcher Controller für welche URL zuständig ist
app.UseRouting();

// Aktiviert die Cookie-Authentifizierung
// → liest das Cookie aus dem Request und setzt den User (HttpContext.User)
// MUSS nach UseRouting() und vor UseAuthorization() stehen!
app.UseAuthentication();

// Aktiviert die Autorisierungsprüfung
// → prüft ob der User [Authorize]-geschützte Seiten besuchen darf
// Wenn nicht eingeloggt → Weiterleitung zu /Account/Login
app.UseAuthorization();

// Definiert das URL-Muster für alle Controller:
// {controller} → Name des Controllers  (z.B. "Games"     → GamesController)
// {action}     → Name der Methode      (z.B. "Dashboard" → Dashboard())
// {id?}        → optionaler Parameter  (z.B. /Games/Edit/5 → id = 5)
// Default: Startseite geht zu HomeController.Index()
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Startet die App und wartet auf HTTP-Anfragen
// Blockiert bis die App beendet wird (z.B. durch Strg+C oder VS Stop)
app.Run();

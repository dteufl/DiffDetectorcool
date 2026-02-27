// ============================================================
//  HomeController.cs  –  Startseiten-Controller
//  Leitet den User von der Startseite (/) direkt zum Dashboard weiter.
// ============================================================

// Importiert ASP.NET MVC → brauchen wir für Controller und IActionResult
using Microsoft.AspNetCore.Mvc;

// Alle Klassen gehören zum Namespace DiffDetector.Controllers
namespace DiffDetector.Controllers
{
    // Controller-Klasse erbt von "Controller" (ASP.NET MVC Basisklasse)
    // → bekommt dadurch Methoden wie View(), RedirectToAction(), etc.
    public class HomeController : Controller
    {
        // Index() = Standardmethode, wird aufgerufen wenn jemand "/" öffnet
        // IActionResult = Rückgabetyp für alle Controller-Methoden
        //   → kann eine View, eine Weiterleitung, ein Fehler, etc. sein
        public IActionResult Index()
        {
            // RedirectToAction leitet den Browser weiter zu einer anderen Action
            // "Dashboard" = Name der Methode im GamesController
            // "Games"     = Name des Controllers (ohne "Controller" am Ende)
            // → Browser wird zu /Games/Dashboard weitergeleitet
            return RedirectToAction("Dashboard", "Games");
        }
    }
}

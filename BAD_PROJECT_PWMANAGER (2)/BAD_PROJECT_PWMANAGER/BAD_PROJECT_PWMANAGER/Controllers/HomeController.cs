using System.Diagnostics;
using BAD_PROJECT_PWMANAGER.Models;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace BAD_PROJECT_PWMANAGER.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated == true && User.IsInRole("Admin"))
            {
                return RedirectToAction("Dashboard", "Admin");
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SetLanguage(string culture, string returnUrl)
        {
            var supportedCultures = new[] { "nl", "en" };

            if (!supportedCultures.Contains(culture))
            {
                culture = "nl";
            }

            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    IsEssential = true,
                    SameSite = SameSiteMode.Lax
                });

            if (string.IsNullOrWhiteSpace(returnUrl) || !Url.IsLocalUrl(returnUrl))
            {
                returnUrl = Url.Action(nameof(Index), "Home") ?? "/";
            }

            return LocalRedirect(returnUrl);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

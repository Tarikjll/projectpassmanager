using Application.Interfaces;
using BAD_PROJECT_PWMANAGER.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BAD_PROJECT_PWMANAGER.Controllers
{
    [Authorize(Roles = "PremiumUser")]
    public class PasswordGeneratorController : Controller
    {
        private readonly IPasswordGeneratorService _passwordGeneratorService;

        public PasswordGeneratorController(IPasswordGeneratorService passwordGeneratorService)
        {
            _passwordGeneratorService = passwordGeneratorService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var model = new PasswordGeneratorViewModel
            {
                Length = 16,
                IncludeUppercase = true,
                IncludeLowercase = true,
                IncludeNumbers = true,
                IncludeSymbols = true,
                ExcludeSimilarCharacters = false,
                ExcludeAmbiguousSymbols = false
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(PasswordGeneratorViewModel model)
        {
            if (!model.IncludeUppercase &&
                !model.IncludeLowercase &&
                !model.IncludeNumbers &&
                !model.IncludeSymbols)
            {
                ModelState.AddModelError("", "Selecteer minstens één type teken.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            model.GeneratedPassword = _passwordGeneratorService.GeneratePassword(
                model.Length,
                model.IncludeUppercase,
                model.IncludeLowercase,
                model.IncludeNumbers,
                model.IncludeSymbols,
                model.ExcludeSimilarCharacters,
                model.ExcludeAmbiguousSymbols
            );

            return View(model);
        }
    }
}

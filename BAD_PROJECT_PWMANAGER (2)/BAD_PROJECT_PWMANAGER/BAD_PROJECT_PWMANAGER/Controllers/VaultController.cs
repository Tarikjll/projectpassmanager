using Application.Interfaces;
using BAD_PROJECT_PWMANAGER.ViewModels.Vault;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Domain.Entities;
namespace BAD_PROJECT_PWMANAGER.Controllers;

[Authorize]
public class VaultController : Controller
{
    private readonly IVaultService _vaultService;
    private readonly UserManager<ApplicationUser> _userManager;

    public VaultController(
        IVaultService vaultService,
        UserManager<ApplicationUser> userManager)
    {
        _vaultService = vaultService;
        _userManager = userManager;
    }

    public IActionResult Index()
    {
        string userId = _userManager.GetUserId(User)!;

        var vaults = _vaultService.GetVaultsForUser(userId)
            .Select(v => new VaultListItemViewModel
            {
                Id = v.Id,
                Name = v.Name,
                Description = v.Description,
                CreatedAt = v.CreatedAt
            })
            .ToList();

        return View(vaults);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
        {
            return Unauthorized();
        }

        bool isPremiumUser = await _userManager.IsInRoleAsync(user, "PremiumUser");

        if (!isPremiumUser)
        {
            var currentVaultCount = _vaultService
                .GetVaultsForUser(user.Id)
                .Count();

            if (currentVaultCount >= 3)
            {
                TempData["ErrorMessage"] =
                    "Free users kunnen maximaal 3 vaults aanmaken. Upgrade naar Premium voor onbeperkte vaults.";

                return RedirectToAction(nameof(Index));
            }
        }

        return View(new VaultFormViewModel());
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(VaultFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);

        if (user == null)
        {
            return Unauthorized();
        }

        string userId = user.Id;
        bool isPremiumUser = await _userManager.IsInRoleAsync(user, "PremiumUser");

        try
        {
            _vaultService.CreateVault(
                model.Name,
                model.Description,
                userId,
                isPremiumUser);

            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpGet]
    public IActionResult Edit(int id)
    {
        string userId = _userManager.GetUserId(User)!;

        var vault = _vaultService.GetVaultForUser(id, userId);

        if (vault == null)
        {
            return NotFound();
        }

        var model = new VaultFormViewModel
        {
            Id = vault.Id,
            Name = vault.Name,
            Description = vault.Description
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(VaultFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        string userId = _userManager.GetUserId(User)!;

        _vaultService.UpdateVault(model.Id, model.Name, model.Description, userId);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(int id)
    {
        string userId = _userManager.GetUserId(User)!;

        _vaultService.DeleteVault(id, userId);

        return RedirectToAction(nameof(Index));
    }
}
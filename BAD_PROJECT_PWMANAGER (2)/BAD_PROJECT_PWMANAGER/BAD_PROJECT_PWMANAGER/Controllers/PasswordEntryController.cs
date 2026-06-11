using Application.Interfaces;
using BAD_PROJECT_PWMANAGER.ViewModels.PasswordEntry;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Domain.Entities;
using System.Text;

namespace BAD_PROJECT_PWMANAGER.Controllers;

[Authorize]
public class PasswordEntryController : Controller
{
    private readonly IPasswordEntryService _passwordEntryService;
    private readonly IPasswordStrengthService _passwordStrengthService;
    private readonly IPasswordEncryptionService _passwordEncryptionService;
    private readonly UserManager<ApplicationUser> _userManager;

    public PasswordEntryController(
        IPasswordEntryService passwordEntryService,
        IPasswordStrengthService passwordStrengthService,
        IPasswordEncryptionService passwordEncryptionService,
        UserManager<ApplicationUser> userManager)
    {
        _passwordEntryService = passwordEntryService;
        _passwordStrengthService = passwordStrengthService;
        _passwordEncryptionService = passwordEncryptionService;
        _userManager = userManager;
    }

    public IActionResult Index(int vaultId)
    {

        string userId = _userManager.GetUserId(User)!;

        var currentVaultEntries = _passwordEntryService
            .GetEntriesForVault(vaultId, userId)
            .ToList();

      
        bool isPremiumUser = User.IsInRole("PremiumUser");

        var allUserEntries = isPremiumUser
            ? _passwordEntryService.GetEntriesForUser(userId).ToList()
            : new List<PasswordEntry>();

        var reusedPasswordGroups = allUserEntries
            .Where(e => !string.IsNullOrWhiteSpace(e.PasswordHash))
            .GroupBy(e => e.PasswordHash)
            .Where(group => group.Count() > 1)
            .ToDictionary(
                group => group.Key,
                group => group.ToList()

            );

        var entries = currentVaultEntries
            .Select(e =>
            {
                var reusedWithPlatforms = new List<string>();

                if (isPremiumUser &&
                    !string.IsNullOrWhiteSpace(e.PasswordHash) &&
                    reusedPasswordGroups.TryGetValue(e.PasswordHash, out var reusedGroup))
                {
                    reusedWithPlatforms = reusedGroup
                        .Where(otherEntry => otherEntry.Id != e.Id)
                        .Select(otherEntry => otherEntry.Platform)
                        .Distinct()
                        .ToList();
                }

                var lastChangedAt = e.UpdatedAt ?? e.CreatedAt;
                var passwordAgeInDays = (DateTime.UtcNow - lastChangedAt).Days;

                return new PasswordEntryListItemViewModel
                {
                    Id = e.Id,
                    VaultId = e.VaultId,
                    Platform = e.Platform,
                    Username = e.Username,
                    Url = e.Url,
                    StrengthLabel = _passwordStrengthService.GetStrengthLabel(e.StrengthScore),
                    CreatedAt = e.CreatedAt,

                    IsPasswordReused = reusedWithPlatforms.Any(),
                    ReusedWithPlatforms = reusedWithPlatforms,


                    IsPasswordOld = isPremiumUser && passwordAgeInDays >= 90,
                    PasswordAgeInDays = passwordAgeInDays
                };
            })
            .ToList();


        ViewBag.VaultId = vaultId;

        return View(entries);
    }

    [HttpGet]
    public IActionResult Create(int vaultId)
    {
        var model = new PasswordEntryFormViewModel
        {
            VaultId = vaultId
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PasswordEntryFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        string userId = _userManager.GetUserId(User)!;
        bool isPremiumUser = await _userManager.IsInRoleAsync(await _userManager.GetUserAsync(User), "PremiumUser");

        try
        {
            _passwordEntryService.CreateEntry(
                model.VaultId,
                model.Platform,
                model.Username,
                model.Url,
                model.PlainPassword,
                model.Notes,
                userId,
                isPremiumUser);

            return RedirectToAction(nameof(Index), new { vaultId = model.VaultId });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }
    [HttpGet]
    public IActionResult Details(int id)
    {
        string userId = _userManager.GetUserId(User)!;

        var entry = _passwordEntryService.GetEntryForUser(id, userId);

        if (entry == null)
        {
            return NotFound();
        }

        bool isPremiumUser = User.IsInRole("PremiumUser");

        var reusedWithPlatforms = new List<string>();

        if (isPremiumUser && !string.IsNullOrWhiteSpace(entry.PasswordHash))
        {
            reusedWithPlatforms = _passwordEntryService
                .GetEntriesForUser(userId)
                .Where(otherEntry =>
                    otherEntry.Id != entry.Id &&
                    otherEntry.PasswordHash == entry.PasswordHash)
                .Select(otherEntry => otherEntry.Platform)
                .Distinct()
                .ToList();
        }

        var lastChangedAt = entry.UpdatedAt ?? entry.CreatedAt;
        var passwordAgeInDays = (DateTime.UtcNow - lastChangedAt).Days;

        var model = new PasswordEntryDetailViewModel
        {
            Id = entry.Id,
            VaultId = entry.VaultId,
            Platform = entry.Platform,
            Username = entry.Username,
            Url = entry.Url,
            Password = _passwordEncryptionService.Decrypt(entry.EncryptedPassword),
            Notes = entry.Notes,
            StrengthLabel = _passwordStrengthService.GetStrengthLabel(entry.StrengthScore),
            CreatedAt = entry.CreatedAt,
            UpdatedAt = entry.UpdatedAt,

            IsPasswordReused = isPremiumUser && reusedWithPlatforms.Any(),
            ReusedWithPlatforms = reusedWithPlatforms,

            IsPasswordOld = isPremiumUser && passwordAgeInDays >= 90,
            PasswordAgeInDays = passwordAgeInDays,
            HistoryItems = isPremiumUser
                ? _passwordEntryService
                    .GetHistoryForEntry(id, userId)
                    .Select(h => new PasswordHistoryItemViewModel
                    {
                        Id = h.Id,
                        OldPassword = _passwordEncryptionService.Decrypt(h.OldEncryptedPassword),
                        ChangedAt = h.ChangedAt
                    })
                    .ToList()
                : new List<PasswordHistoryItemViewModel>()
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult Edit(int id)
    {
        string userId = _userManager.GetUserId(User)!;

        var entry = _passwordEntryService.GetEntryForUser(id, userId);

        if (entry == null)
        {
            return NotFound();
        }

        var model = new PasswordEntryFormViewModel
        {
            Id = entry.Id,
            VaultId = entry.VaultId,
            Platform = entry.Platform,
            Username = entry.Username,
            Url = entry.Url,
            Notes = entry.Notes
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(PasswordEntryFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        string userId = _userManager.GetUserId(User)!;
        bool isPremiumUser = await _userManager.IsInRoleAsync(await _userManager.GetUserAsync(User), "PremiumUser");

        try
        {
            _passwordEntryService.UpdateEntry(
                model.Id,
                model.Platform,
                model.Username,
                model.Url,
                model.PlainPassword,
                model.Notes,
                userId,
                isPremiumUser);

            return RedirectToAction(nameof(Index), new { vaultId = model.VaultId });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(int id, int vaultId)
    {
        string userId = _userManager.GetUserId(User)!;

        try
        {
            _passwordEntryService.DeleteEntry(id, userId);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Index), new { vaultId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "PremiumUser")]
    public async Task<IActionResult> Export(int vaultId, string accountPassword)
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null || string.IsNullOrWhiteSpace(accountPassword) ||
            !await _userManager.CheckPasswordAsync(user, accountPassword))
        {
            TempData["ExportError"] = "Export mislukt. Controleer je accountwachtwoord.";
            return RedirectToAction(nameof(Index), new { vaultId });
        }

        string userId = _userManager.GetUserId(User)!;
        var entries = _passwordEntryService
            .GetEntriesForVault(vaultId, userId)
            .ToList();

        var csv = new StringBuilder();
        csv.AppendLine("Platform,Gebruikersnaam,URL,Wachtwoord,Notities,Sterkte,Aangemaakt op,Laatst gewijzigd");

        foreach (var entry in entries)
        {
            var values = new[]
            {
                entry.Platform,
                entry.Username,
                entry.Url ?? string.Empty,
                _passwordEncryptionService.Decrypt(entry.EncryptedPassword),
                entry.Notes ?? string.Empty,
                _passwordStrengthService.GetStrengthLabel(entry.StrengthScore),
                entry.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                entry.UpdatedAt?.ToLocalTime().ToString("dd/MM/yyyy HH:mm") ?? string.Empty
            };

            csv.AppendLine(string.Join(",", values.Select(EscapeCsv)));
        }

        var fileName = $"passmanager-vault-{vaultId}-{DateTime.UtcNow:yyyyMMddHHmm}.csv";
        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", fileName);
    }

    private static string EscapeCsv(string value)
    {
        var escaped = value.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }

    [Authorize(Roles = "PremiumUser")]
    [HttpGet]
    public IActionResult History(int id)
    {
        string userId = _userManager.GetUserId(User)!;

        var entry = _passwordEntryService.GetEntryForUser(id, userId);

        if (entry == null)
        {
            return NotFound();
        }

        var historyItems = _passwordEntryService.GetHistoryForEntry(id, userId);

        var model = new PasswordHistoryViewModel
        {
            PasswordEntryId = entry.Id,
            VaultId = entry.VaultId,
            Platform = entry.Platform,
            Username = entry.Username,
            Items = historyItems.Select(h => new PasswordHistoryItemViewModel
            {
                Id = h.Id,
                OldPassword = _passwordEncryptionService.Decrypt(h.OldEncryptedPassword),
                ChangedAt = h.ChangedAt
            }).ToList()
        };

        return View(model);
    }


}

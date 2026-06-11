using Application.Interfaces;
using BAD_PROJECT_PWMANAGER.ViewModels.Admin;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BAD_PROJECT_PWMANAGER.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private static readonly string[] ManagedRoles = ["Admin", "PremiumUser", "FreeUser"];

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUnitOfWork _unitOfWork;

    public AdminController(
        UserManager<ApplicationUser> userManager,
        IUnitOfWork unitOfWork)
    {
        _userManager = userManager;
        _unitOfWork = unitOfWork;
    }

    public async Task<IActionResult> Users()
    {
        var users = _userManager.Users.ToList();
        var model = new List<UserListViewModel>();

        foreach (var user in users)
        {
            model.Add(new UserListViewModel
            {
                UserId = user.Id,
                Email = user.Email ?? "",
                Roles = await _userManager.GetRolesAsync(user)
            });
        }

        return View(model);
    }

    public IActionResult AuditLogs(string? entity = "all", string? operation = "all", string? q = null)
    {
        var allLogs = _unitOfWork.AuditLogs.GetAll().ToList();

        var filteredLogs = allLogs.Where(log =>
        {
            var matchesEntity = string.IsNullOrWhiteSpace(entity) ||
                                entity.Equals("all", StringComparison.OrdinalIgnoreCase) ||
                                log.EntityName.Equals(entity, StringComparison.OrdinalIgnoreCase);

            var operationCode = GetOperationCode(log.Action);
            var matchesOperation = string.IsNullOrWhiteSpace(operation) ||
                                   operation.Equals("all", StringComparison.OrdinalIgnoreCase) ||
                                   operationCode.Equals(operation, StringComparison.OrdinalIgnoreCase);

            var matchesQuery = string.IsNullOrWhiteSpace(q) ||
                               log.Action.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                               log.EntityName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                               log.UserId.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                               log.EntityId?.ToString().Contains(q, StringComparison.OrdinalIgnoreCase) == true;

            return matchesEntity && matchesOperation && matchesQuery;
        })
        .OrderByDescending(log => log.CreatedAt)
        .ToList();

        var logs = filteredLogs.Take(100).ToList();

        var userIds = logs
            .Where(log => !string.IsNullOrWhiteSpace(log.UserId))
            .Select(log => log.UserId)
            .Distinct()
            .ToList();

        var users = _userManager.Users
            .Where(user => userIds.Contains(user.Id))
            .ToList();

        var usersById = users.ToDictionary(user => user.Id, user => user);

        var model = logs.Select(log => new AuditLogListItemViewModel
        {
            Id = log.Id,
            UserId = log.UserId,
            UserEmail = usersById.TryGetValue(log.UserId, out var user)
                ? user.Email ?? user.UserName ?? "Onbekende gebruiker"
                : "Onbekende gebruiker",
            Action = log.Action,
            OperationLabel = GetOperationKey(log.Action),
            EntityName = log.EntityName,
            EntityId = log.EntityId,
            CreatedAt = log.CreatedAt
        }).ToList();

        return View(new AuditLogIndexViewModel
        {
            Logs = model,
            SearchTerm = q ?? string.Empty,
            EntityFilter = entity ?? "all",
            OperationFilter = operation ?? "all",
            TotalCount = allLogs.Count,
            FilteredCount = filteredLogs.Count
        });
    }

    private static string GetOperationCode(string action)
    {
        if (action.Contains("aangemaakt", StringComparison.OrdinalIgnoreCase))
        {
            return "create";
        }

        if (action.Contains("gewijzigd", StringComparison.OrdinalIgnoreCase))
        {
            return "update";
        }

        if (action.Contains("verwijderd", StringComparison.OrdinalIgnoreCase))
        {
            return "delete";
        }

        if (action.Contains("werd", StringComparison.OrdinalIgnoreCase))
        {
            return "role";
        }

        return "other";
    }

    private static string GetOperationKey(string action)
    {
        return GetOperationCode(action) switch
        {
            "create" => "Admin.OperationCreate",
            "update" => "Admin.OperationUpdate",
            "delete" => "Admin.OperationDelete",
            "role" => "Admin.OperationRole",
            _ => "Admin.OperationOther"
        };
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetRole(string userId, string role)
    {
        if (!ManagedRoles.Contains(role))
        {
            return BadRequest();
        }

        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return NotFound();
        }

        var adminUserId = _userManager.GetUserId(User) ?? string.Empty;

        if (user.Id == adminUserId && role != "Admin")
        {
            return RedirectToAction(nameof(Users));
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        var rolesToRemove = currentRoles.Where(ManagedRoles.Contains).ToList();

        if (rolesToRemove.Count > 0)
        {
            await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
        }

        if (!await _userManager.IsInRoleAsync(user, role))
        {
            await _userManager.AddToRoleAsync(user, role);
        }

        _unitOfWork.AuditLogs.Add(new AuditLog
        {
            UserId = adminUserId,
            Action = $"Gebruiker {user.Email ?? user.UserName} werd {role} gemaakt",
            EntityName = "ApplicationUser",
            EntityId = null,
            CreatedAt = DateTime.UtcNow
        });

        _unitOfWork.Save();

        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MakePremium(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return NotFound();
        }

        if (!await _userManager.IsInRoleAsync(user, "PremiumUser"))
        {
            await _userManager.AddToRoleAsync(user, "PremiumUser");
        }

        if (await _userManager.IsInRoleAsync(user, "FreeUser"))
        {
            await _userManager.RemoveFromRoleAsync(user, "FreeUser");
        }

        var adminUserId = _userManager.GetUserId(User) ?? string.Empty;

        _unitOfWork.AuditLogs.Add(new AuditLog
        {
            UserId = adminUserId,
            Action = $"Gebruiker {user.Email ?? user.UserName} werd PremiumUser gemaakt",
            EntityName = "ApplicationUser",
            EntityId = null,
            CreatedAt = DateTime.UtcNow
        });

        _unitOfWork.Save();

        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MakeFree(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return NotFound();
        }

        if (!await _userManager.IsInRoleAsync(user, "FreeUser"))
        {
            await _userManager.AddToRoleAsync(user, "FreeUser");
        }

        if (await _userManager.IsInRoleAsync(user, "PremiumUser"))
        {
            await _userManager.RemoveFromRoleAsync(user, "PremiumUser");
        }

        var adminUserId = _userManager.GetUserId(User) ?? string.Empty;

        _unitOfWork.AuditLogs.Add(new AuditLog
        {
            UserId = adminUserId,
            Action = $"Gebruiker {user.Email ?? user.UserName} werd FreeUser gemaakt",
            EntityName = "ApplicationUser",
            EntityId = null,
            CreatedAt = DateTime.UtcNow
        });

        _unitOfWork.Save();

        return RedirectToAction(nameof(Users));
    }
}
  

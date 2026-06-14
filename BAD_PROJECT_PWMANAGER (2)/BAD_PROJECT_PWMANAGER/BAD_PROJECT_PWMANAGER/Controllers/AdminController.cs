using Application.Interfaces;
using BAD_PROJECT_PWMANAGER.ViewModels.Admin;
using Domain.Entities;
using Infrastructure.Data;
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
    private readonly ApplicationDbContext _dbContext;

    public AdminController(
        UserManager<ApplicationUser> userManager,
        IUnitOfWork unitOfWork,
        ApplicationDbContext dbContext)
    {
        _userManager = userManager;
        _unitOfWork = unitOfWork;
        _dbContext = dbContext;
    }

    public async Task<IActionResult> Dashboard()
    {
        return View(await BuildDashboardModelAsync());
    }

    public IActionResult Index()
    {
        return RedirectToAction(nameof(Dashboard));
    }

    public async Task<IActionResult> Users()
    {
        return View(await BuildDashboardModelAsync());
    }

    private async Task<AdminDashboardViewModel> BuildDashboardModelAsync()
    {
        ViewBag.CurrentUserId = _userManager.GetUserId(User) ?? string.Empty;

        var users = _userManager.Users.ToList();
        var model = new List<UserListViewModel>();

        var adminUsers = 0;
        var premiumUsers = 0;
        var freeUsers = 0;

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);

            if (roles.Contains("Admin"))
            {
                adminUsers++;
            }
            else if (roles.Contains("PremiumUser"))
            {
                premiumUsers++;
            }
            else
            {
                freeUsers++;
            }

            model.Add(new UserListViewModel
            {
                UserId = user.Id,
                Email = user.Email ?? "",
                Roles = roles,
                IsBanned = user.IsBanned,
                BannedUntilUtc = user.BannedUntilUtc,
                BanReason = user.BanReason
            });
        }

        var now = DateTime.UtcNow;
        var allAuditLogs = _unitOfWork.AuditLogs.GetAll().ToList();
        var recentAuditLogs = allAuditLogs
            .OrderByDescending(log => log.CreatedAt)
            .Take(5)
            .Select(log => new AuditLogListItemViewModel
            {
                Id = log.Id,
                UserId = log.UserId,
                UserEmail = _userManager.Users.FirstOrDefault(user => user.Id == log.UserId)?.Email
                    ?? _userManager.Users.FirstOrDefault(user => user.Id == log.UserId)?.UserName
                    ?? "Onbekende gebruiker",
                Action = log.Action,
                OperationLabel = GetOperationKey(log.Action),
                EntityName = log.EntityName,
                EntityId = log.EntityId,
                CreatedAt = log.CreatedAt
            })
            .ToList();

        var recentLoginAttempts = _dbContext.LoginAttempts
            .OrderByDescending(attempt => attempt.AttemptedAtUtc)
            .Take(5)
            .Select(attempt => new LoginAttemptListItemViewModel
            {
                Id = attempt.Id,
                LoginIdentifier = attempt.LoginIdentifier,
                Succeeded = attempt.Succeeded,
                FailureReason = attempt.FailureReason,
                IpAddress = attempt.IpAddress,
                AttemptedAtUtc = attempt.AttemptedAtUtc
            })
            .ToList();

        return new AdminDashboardViewModel
        {
            Users = model,
            TotalUsers = users.Count,
            AdminUsers = adminUsers,
            PremiumUsers = premiumUsers,
            FreeUsers = freeUsers,
            BannedUsers = users.Count(user => user.IsBanned),
            VaultCount = _dbContext.Vaults.Count(),
            PasswordCount = _dbContext.PasswordEntries.Count(),
            ExportCount = _dbContext.PasswordExports.Count(),
            LoginAttemptsLast7Days = _dbContext.LoginAttempts.Count(attempt => attempt.AttemptedAtUtc >= now.AddDays(-7)),
            FailedLoginAttemptsLast7Days = _dbContext.LoginAttempts.Count(attempt => !attempt.Succeeded && attempt.AttemptedAtUtc >= now.AddDays(-7)),
            RecentLoginAttempts = recentLoginAttempts,
            RecentAuditLogs = recentAuditLogs,
            AuditLogsLast7Days = allAuditLogs.Count(log => log.CreatedAt >= now.AddDays(-7)),
            AuditCreateCount = allAuditLogs.Count(log => GetOperationCode(log.Action) == "create"),
            AuditUpdateCount = allAuditLogs.Count(log => GetOperationCode(log.Action) == "update"),
            AuditDeleteCount = allAuditLogs.Count(log => GetOperationCode(log.Action) == "delete")
        };
    }

    public IActionResult AuditLogs(string? entity = "all", string? operation = "all", string? q = null, int page = 1)
    {
        const int pageSize = 25;

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

            return matchesEntity && matchesOperation;
        })
        .OrderByDescending(log => log.CreatedAt)
        .ToList();

        var userIds = filteredLogs
            .Where(log => !string.IsNullOrWhiteSpace(log.UserId))
            .Select(log => log.UserId)
            .Distinct()
            .ToList();

        var usersById = _userManager.Users
            .Where(user => userIds.Contains(user.Id))
            .ToDictionary(user => user.Id, user => user);

        var modelLogs = filteredLogs.Select(log => new AuditLogListItemViewModel
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

        if (!string.IsNullOrWhiteSpace(q))
        {
            modelLogs = modelLogs.Where(log =>
                log.UserEmail.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                log.Action.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                log.EntityName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                log.OperationLabel.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                log.EntityId?.ToString().Contains(q, StringComparison.OrdinalIgnoreCase) == true)
                .ToList();
        }

        var totalCount = modelLogs.Count;
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        page = Math.Clamp(page, 1, totalPages);

        var pageItems = modelLogs
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return View(new AuditLogIndexViewModel
        {
            Logs = pageItems,
            SearchTerm = q ?? string.Empty,
            EntityFilter = entity ?? "all",
            OperationFilter = operation ?? "all",
            TotalCount = allLogs.Count,
            FilteredCount = modelLogs.Count,
            CurrentPage = page,
            TotalPages = totalPages,
            PageSize = pageSize
        });
    }

    private static string GetOperationCode(string action)
    {
        if (action.Contains("aangemaakt", StringComparison.OrdinalIgnoreCase))
        {
            return "create";
        }

        if (action.Contains("verbannen", StringComparison.OrdinalIgnoreCase) ||
            action.Contains("opnieuw toegelaten", StringComparison.OrdinalIgnoreCase) ||
            action.Contains("ban", StringComparison.OrdinalIgnoreCase))
        {
            return "ban";
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
            "ban" => "Admin.OperationBan",
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BanUser(string userId, int banDurationDays, string banReason)
    {
        if (banDurationDays <= 0 || string.IsNullOrWhiteSpace(banReason))
        {
            return BadRequest();
        }

        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return NotFound();
        }

        var adminUserId = _userManager.GetUserId(User) ?? string.Empty;

        if (user.Id == adminUserId)
        {
            TempData["AdminMessage"] = "Je kan je eigen account niet verbannen.";
            return RedirectToAction(nameof(Users));
        }

        user.BannedUntilUtc = DateTimeOffset.UtcNow.AddDays(banDurationDays);
        user.BanReason = banReason.Trim();

        await _userManager.UpdateAsync(user);
        TempData["AdminMessage"] = $"Gebruiker {user.Email ?? user.UserName} is verbannen tot {user.BannedUntilUtc:dd/MM/yyyy HH:mm}.";

        _unitOfWork.AuditLogs.Add(new AuditLog
        {
            UserId = adminUserId,
            Action = $"Gebruiker {user.Email ?? user.UserName} werd verbannen voor {banDurationDays} dagen: {user.BanReason}",
            EntityName = "ApplicationUser",
            EntityId = null,
            CreatedAt = DateTime.UtcNow
        });

        _unitOfWork.Save();

        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnbanUser(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return NotFound();
        }

        user.BannedUntilUtc = null;
        user.BanReason = null;

        await _userManager.UpdateAsync(user);
        TempData["AdminMessage"] = $"Verbanning voor {user.Email ?? user.UserName} is opgeheven.";

        var adminUserId = _userManager.GetUserId(User) ?? string.Empty;

        _unitOfWork.AuditLogs.Add(new AuditLog
        {
            UserId = adminUserId,
            Action = $"Gebruiker {user.Email ?? user.UserName} werd opnieuw toegelaten",
            EntityName = "ApplicationUser",
            EntityId = null,
            CreatedAt = DateTime.UtcNow
        });

        _unitOfWork.Save();

        return RedirectToAction(nameof(Users));
    }
}

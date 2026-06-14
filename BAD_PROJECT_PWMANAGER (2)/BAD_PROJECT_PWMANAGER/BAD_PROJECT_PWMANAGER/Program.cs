using Application.Interfaces;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Application.Security;
using Domain.Entities;
using Application.Services;
using Microsoft.AspNetCore.Localization;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<Infrastructure.Data.ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Identity + rollen
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>        
{
    options.SignIn.RequireConfirmedAccount = true;

    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;

    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<Infrastructure.Data.ApplicationDbContext>();

// MVC + Razor Pages voor Identity
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services
    .AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();
builder.Services
    .AddRazorPages()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();

// Eigen services

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IVaultService, VaultService>();
builder.Services.AddScoped<IPasswordGeneratorService, PasswordGeneratorService>();
//security services
builder.Services.AddScoped<IPasswordEncryptionService, PasswordEncryptionService>();
builder.Services.AddScoped<IPasswordHashService, PasswordHashService>();
builder.Services.AddScoped<IPasswordStrengthService, PasswordStrengthService>();

//password entry
builder.Services.AddScoped<IPasswordEntryService, PasswordEntryService>();



var app = builder.Build();

// Database migreren en demo data seeden
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<Infrastructure.Data.ApplicationDbContext>();
    await db.Database.MigrateAsync();
    await SeedDemoDataAsync(scope.ServiceProvider);
}


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();

var supportedCultures = new[]
{
    new CultureInfo("nl"),
    new CultureInfo("en")
};

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("nl"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});
    
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();

async Task SeedDemoDataAsync(IServiceProvider serviceProvider)
{
    var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var db = serviceProvider.GetRequiredService<Infrastructure.Data.ApplicationDbContext>();
    var passwordHashService = serviceProvider.GetRequiredService<IPasswordHashService>();
    var passwordEncryptionService = serviceProvider.GetRequiredService<IPasswordEncryptionService>();
    var passwordStrengthService = serviceProvider.GetRequiredService<IPasswordStrengthService>();

    string[] roles = { "Admin", "FreeUser", "PremiumUser" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    var freeUser = await EnsureDemoUserAsync(
        userManager,
        "free.demo@pwmanager.be",
        "Free",
        "Demo",
        "FreeDemo123!",
        "FreeUser");

    var premiumUser = await EnsureDemoUserAsync(
        userManager,
        "premium.demo@pwmanager.be",
        "Premium",
        "Demo",
        "PremiumDemo123!",
        "PremiumUser");

    await SeedFreeUserDataAsync(db, passwordHashService, passwordEncryptionService, passwordStrengthService, freeUser);
    await SeedPremiumUserDataAsync(db, passwordHashService, passwordEncryptionService, passwordStrengthService, premiumUser);
}

async Task<ApplicationUser> EnsureDemoUserAsync(
    UserManager<ApplicationUser> userManager,
    string email,
    string firstName,
    string lastName,
    string password,
    string role)
{
    var user = await userManager.FindByEmailAsync(email);

    if (user == null)
    {
        user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = firstName,
            LastName = lastName
        };

        var result = await userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            var error = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Kan demo-gebruiker {email} niet aanmaken: {error}");
        }
    }

    if (!await userManager.IsInRoleAsync(user, role))
    {
        await userManager.AddToRoleAsync(user, role);
    }

    return user;
}

async Task SeedFreeUserDataAsync(
    Infrastructure.Data.ApplicationDbContext db,
    IPasswordHashService hashService,
    IPasswordEncryptionService encryptionService,
    IPasswordStrengthService strengthService,
    ApplicationUser user)
{
    const string personalVaultName = "Free Demo - Personal";
    const string workVaultName = "Free Demo - Work";

    var existingVault = await db.Vaults
        .AnyAsync(v => v.UserId == user.Id && (v.Name == personalVaultName || v.Name == workVaultName));

    if (existingVault)
    {
        return;
    }

    var now = DateTime.UtcNow;
    var created = now.AddDays(-14);

    var personalVault = new Vault
    {
        Name = personalVaultName,
        Description = "Vrije gebruiker demo voor de limiet van 3 vaults.",
        UserId = user.Id,
        CreatedAt = created
    };

    var workVault = new Vault
    {
        Name = workVaultName,
        Description = "Tweede demo vault om de gratis limiet bijna te raken.",
        UserId = user.Id,
        CreatedAt = created.AddDays(1)
    };

    db.Vaults.AddRange(personalVault, workVault);
    await db.SaveChangesAsync();

    var entries = new[]
    {
        CreateEntry(personalVault, "Gmail", "free.demo@gmail.com", "https://mail.google.com", "FreeDemoMail1!", "Primair e-mailadres", created.AddDays(-9)),
        CreateEntry(personalVault, "Microsoft", "free.demo@outlook.com", "https://account.microsoft.com", "FreeDemoOffice2@", "Werkaccount", created.AddDays(-8)),
        CreateEntry(personalVault, "Spotify", "free.demo", "https://spotify.com", "FreeDemoMusic3#", null, created.AddDays(-7)),
        CreateEntry(personalVault, "LinkedIn", "free.demo", "https://linkedin.com", "FreeDemoCareer4$", null, created.AddDays(-6)),
        CreateEntry(personalVault, "Netflix", "free.demo", "https://netflix.com", "FreeDemoVideo5%", null, created.AddDays(-5)),
        CreateEntry(workVault, "GitHub", "free.demo", "https://github.com", "FreeDemoCode6^", "Code repository", created.AddDays(-4)),
        CreateEntry(workVault, "Slack", "free.demo", "https://slack.com", "FreeDemoChat7&", null, created.AddDays(-3)),
        CreateEntry(workVault, "Canva", "free.demo", "https://canva.com", "FreeDemoDesign8*", null, created.AddDays(-2)),
        CreateEntry(workVault, "Notion", "free.demo", "https://notion.so", "FreeDemoNote9!", null, created.AddDays(-1))
    };

    db.PasswordEntries.AddRange(entries);

    db.AuditLogs.AddRange(
        new AuditLog { UserId = user.Id, Action = "Free demo data aangemaakt", EntityName = nameof(Vault), EntityId = personalVault.Id, CreatedAt = now.AddDays(-13) },
        new AuditLog { UserId = user.Id, Action = "Free demo data aangemaakt", EntityName = nameof(Vault), EntityId = workVault.Id, CreatedAt = now.AddDays(-12) });

    await db.SaveChangesAsync();

    PasswordEntry CreateEntry(
        Vault vault,
        string platform,
        string username,
        string? url,
        string plainPassword,
        string? notes,
        DateTime createdAt)
    {
        return new PasswordEntry
        {
            VaultId = vault.Id,
            Platform = platform,
            Username = username,
            Url = url,
            EncryptedPassword = encryptionService.Encrypt(plainPassword),
            PasswordHash = hashService.HashPassword(plainPassword),
            Notes = notes,
            StrengthScore = strengthService.CalculateScore(plainPassword),
            CreatedAt = createdAt
        };
    }
}

async Task SeedPremiumUserDataAsync(
    Infrastructure.Data.ApplicationDbContext db,
    IPasswordHashService hashService,
    IPasswordEncryptionService encryptionService,
    IPasswordStrengthService strengthService,
    ApplicationUser user)
{
    const string personalVaultName = "Premium Demo - Personal";
    const string workVaultName = "Premium Demo - Work";

    var existingVault = await db.Vaults
        .AnyAsync(v => v.UserId == user.Id && (v.Name == personalVaultName || v.Name == workVaultName));

    if (existingVault)
    {
        return;
    }

    var now = DateTime.UtcNow;
    var oldDate = now.AddDays(-120);
    var recentDate = now.AddDays(-12);
    var duplicatePassword = "Aurora!2024Strong";

    var personalVault = new Vault
    {
        Name = personalVaultName,
        Description = "Premium demo met oude en hergebruikte wachtwoorden.",
        UserId = user.Id,
        CreatedAt = oldDate
    };

    var workVault = new Vault
    {
        Name = workVaultName,
        Description = "Tweede premium vault om reuse tussen accounts te tonen.",
        UserId = user.Id,
        CreatedAt = oldDate.AddDays(1)
    };

    db.Vaults.AddRange(personalVault, workVault);
    await db.SaveChangesAsync();

    var entries = new[]
    {
        CreateEntry(personalVault, "Facebook", "premium.demo.fb", "https://facebook.com", "weak12", "Zwak wachtwoord", oldDate.AddDays(1)),
        CreateEntry(personalVault, "Outlook", "premium.demo@outlook.com", "https://outlook.com", "Medium12", "Gemiddeld wachtwoord", oldDate.AddDays(2)),
        CreateEntry(personalVault, "Google", "premium.demo@gmail.com", "https://mail.google.com", duplicatePassword, "Sterk wachtwoord, hergebruikt", oldDate.AddDays(3)),
        CreateEntry(workVault, "GitHub", "premium.demo", "https://github.com", duplicatePassword, "Zelfde wachtwoord als Google", recentDate),
        CreateEntry(workVault, "Dropbox", "premium.demo", "https://dropbox.com", "VeryStrong!12345", "Sterk en ouder dan 90 dagen", oldDate.AddDays(4))
    };

    db.PasswordEntries.AddRange(entries);
    await db.SaveChangesAsync();

    var googleEntry = entries[2];

    db.PasswordHistories.Add(new PasswordHistory
    {
        PasswordEntryId = googleEntry.Id,
        OldEncryptedPassword = encryptionService.Encrypt("OldGoogle12!"),
        OldPasswordHash = hashService.HashPassword("OldGoogle12!"),
        ChangedAt = oldDate.AddDays(10)
    });

    db.PasswordExports.Add(new PasswordExport
    {
        PasswordEntryId = entries[3].Id,
        UserId = user.Id,
        DestinationType = "CSV",
        DestinationMasked = "*****@demo.local",
        ExportedAt = recentDate.AddDays(1)
    });

    db.AuditLogs.AddRange(
        new AuditLog { UserId = user.Id, Action = "Premium demo data aangemaakt", EntityName = nameof(Vault), EntityId = personalVault.Id, CreatedAt = oldDate },
        new AuditLog { UserId = user.Id, Action = "Premium demo data aangemaakt", EntityName = nameof(Vault), EntityId = workVault.Id, CreatedAt = oldDate.AddDays(1) });

    await db.SaveChangesAsync();

    PasswordEntry CreateEntry(
        Vault vault,
        string platform,
        string username,
        string? url,
        string plainPassword,
        string? notes,
        DateTime createdAt)
    {
        return new PasswordEntry
        {
            VaultId = vault.Id,
            Platform = platform,
            Username = username,
            Url = url,
            EncryptedPassword = encryptionService.Encrypt(plainPassword),
            PasswordHash = hashService.HashPassword(plainPassword),
            Notes = notes,
            StrengthScore = strengthService.CalculateScore(plainPassword),
            CreatedAt = createdAt
        };
    }
}

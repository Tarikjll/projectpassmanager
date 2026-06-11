using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Application.Interfaces;
using Domain.Entities;
  
namespace Application.Services;

public class PasswordEntryService : IPasswordEntryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordEncryptionService _encryptionService;
    private readonly IPasswordHashService _hashService;
    private readonly IPasswordStrengthService _strengthService;

    public PasswordEntryService(
        IUnitOfWork unitOfWork,
        IPasswordEncryptionService encryptionService,
        IPasswordHashService hashService,
        IPasswordStrengthService strengthService)
    {
        _unitOfWork = unitOfWork;
        _encryptionService = encryptionService;
        _hashService = hashService;
        _strengthService = strengthService;
    }

    public IEnumerable<PasswordEntry> GetEntriesForVault(int vaultId, string userId)
    {
        var vault = _unitOfWork.Vaults
            .GetByCondition(v => v.Id == vaultId && v.UserId == userId);

        if (vault == null)
        {
            throw new InvalidOperationException("Vault niet gevonden of geen toegang.");
        }

        return _unitOfWork.PasswordEntries
            .GetAllByCondition(p => p.VaultId == vaultId)
            .OrderBy(p => p.Platform);
    }

    public PasswordEntry? GetEntryForUser(int id, string userId)
    {
        return _unitOfWork.PasswordEntries
            .GetAllByCondition(p => p.Id == id && p.Vault.UserId == userId)
            .FirstOrDefault();
    }

    public void CreateEntry(
        int vaultId,
        string platform,
        string username,
        string? url,
        string plainPassword,
        string? notes,
        string userId,
        bool isPremiumUser)
    {
        var vault = _unitOfWork.Vaults
            .GetByCondition(v => v.Id == vaultId && v.UserId == userId);

        if (vault == null)
        {
            throw new InvalidOperationException("Vault niet gevonden of geen toegang.");
        }

        int totalPasswords = _unitOfWork.PasswordEntries
            .GetAllByCondition(p => p.Vault.UserId == userId)
            .Count();

        if (!isPremiumUser && totalPasswords >= 10)
        {
            throw new InvalidOperationException("Free users kunnen maximaal 10 wachtwoorden opslaan.");
        }

        string passwordHash = _hashService.HashPassword(plainPassword);

        if (isPremiumUser)
        {
            bool passwordAlreadyUsed = _unitOfWork.PasswordEntries
                .GetAllByCondition(p => p.Vault.UserId == userId && p.PasswordHash == passwordHash)
                .Any();

            
        }

        var entry = new PasswordEntry
        {
            VaultId = vaultId,
            Platform = platform,
            Username = username,
            Url = url,
            EncryptedPassword = _encryptionService.Encrypt(plainPassword),
            PasswordHash = passwordHash,
            Notes = notes,
            StrengthScore = _strengthService.CalculateScore(plainPassword),
            CreatedAt = DateTime.UtcNow
        };

        _unitOfWork.PasswordEntries.Add(entry);
        _unitOfWork.Save();

        _unitOfWork.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = "Password entry aangemaakt",
            EntityName = nameof(PasswordEntry),
            EntityId = entry.Id,
            CreatedAt = DateTime.UtcNow
        });

        _unitOfWork.Save();
    }

    public void UpdateEntry(
        int id,
        string platform,
        string username,
        string? url,
        string plainPassword,
        string? notes,
        string userId,
        bool isPremiumUser)
    {
        var entry = GetEntryForUser(id, userId);

        if (entry == null)
        {
            throw new InvalidOperationException("Password entry niet gevonden of geen toegang.");
        }

        if (isPremiumUser)
        {
            _unitOfWork.PasswordHistories.Add(new PasswordHistory
            {
                PasswordEntryId = entry.Id,
                OldEncryptedPassword = entry.EncryptedPassword,
                OldPasswordHash = entry.PasswordHash,
                ChangedAt = DateTime.UtcNow
            });
        }

        string passwordHash = _hashService.HashPassword(plainPassword);

        entry.Platform = platform;
        entry.Username = username;
        entry.Url = url;
        entry.EncryptedPassword = _encryptionService.Encrypt(plainPassword);
        entry.PasswordHash = passwordHash;
        entry.Notes = notes;
        entry.StrengthScore = _strengthService.CalculateScore(plainPassword);
        entry.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.PasswordEntries.Update(entry);

        _unitOfWork.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = "Password entry gewijzigd",
            EntityName = nameof(PasswordEntry),
            EntityId = entry.Id,
            CreatedAt = DateTime.UtcNow
        });

        _unitOfWork.Save();
    }

    public void DeleteEntry(int id, string userId)
    {
        var entry = GetEntryForUser(id, userId);

        if (entry == null)
        {
            throw new InvalidOperationException("Password entry niet gevonden of geen toegang.");
        }

        _unitOfWork.PasswordEntries.Delete(entry);

        _unitOfWork.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = "Password entry verwijderd",
            EntityName = nameof(PasswordEntry),
            EntityId = entry.Id,
            CreatedAt = DateTime.UtcNow
        });

        _unitOfWork.Save();

    }

    public IEnumerable<PasswordHistory> GetHistoryForEntry(int passwordEntryId, string userId)
    {
        var entry = GetEntryForUser(passwordEntryId, userId);

        if (entry == null)
        {
            throw new InvalidOperationException("Wachtwoord niet gevonden.");
        }

        return _unitOfWork.PasswordHistories
            .GetAll()
            .Where(h => h.PasswordEntryId == passwordEntryId)
            .OrderByDescending(h => h.ChangedAt)
            .ToList();
    }


    public IEnumerable<PasswordEntry> GetEntriesForUser(string userId)
    {
        var vaultIds = _unitOfWork.Vaults
            .GetAllByCondition(v => v.UserId == userId)
            .Select(v => v.Id)
            .ToList();

        if (!vaultIds.Any())
        {
            return new List<PasswordEntry>();
        }   

        return _unitOfWork.PasswordEntries
            .GetAllByCondition(p => vaultIds.Contains(p.VaultId))
            .OrderBy(p => p.Platform)
            .ToList();


    }
}

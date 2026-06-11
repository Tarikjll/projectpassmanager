using Application.Interfaces;
using Domain.Entities;

namespace Application.Services;

public class VaultService : IVaultService
{
    private const int MaxFreeUserVaults = 3;

    private readonly IUnitOfWork _unitOfWork;

    public VaultService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public IEnumerable<Vault> GetVaultsForUser(string userId)
    {
        return _unitOfWork.Vaults
            .GetAllByCondition(v => v.UserId == userId)
            .OrderBy(v => v.Name);
    }

    public Vault? GetVaultForUser(int id, string userId)
    {
        return _unitOfWork.Vaults
            .GetByCondition(v => v.Id == id && v.UserId == userId);
    }

    public void CreateVault(string name, string? description, string userId, bool isPremiumUser)
    {
        if (!isPremiumUser)
        {
            var currentVaultCount = _unitOfWork.Vaults
                .GetAllByCondition(v => v.UserId == userId)
                .Count();

            if (currentVaultCount >= MaxFreeUserVaults)
            {
                throw new InvalidOperationException(
                    $"Free users kunnen maximaal {MaxFreeUserVaults} vaults aanmaken. Upgrade naar Premium voor onbeperkte vaults.");
            }
        }

        var vault = new Vault
        {
            Name = name,
            Description = description,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _unitOfWork.Vaults.Add(vault);
        _unitOfWork.Save();

        _unitOfWork.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = "Vault aangemaakt",
            EntityName = nameof(Vault),
            EntityId = vault.Id,
            CreatedAt = DateTime.UtcNow
        });

        _unitOfWork.Save();
    }

    public void UpdateVault(int id, string name, string? description, string userId)
    {
        var vault = GetVaultForUser(id, userId);

        if (vault == null)
        {
            throw new InvalidOperationException("Vault niet gevonden of geen toegang.");
        }

        vault.Name = name;
        vault.Description = description;

        _unitOfWork.Vaults.Update(vault);

        _unitOfWork.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = "Vault gewijzigd",
            EntityName = nameof(Vault),
            EntityId = vault.Id,
            CreatedAt = DateTime.UtcNow
        });

        _unitOfWork.Save();
    }

    public void DeleteVault(int id, string userId)
    {
        var vault = GetVaultForUser(id, userId);

        if (vault == null)
        {
            throw new InvalidOperationException("Vault niet gevonden of geen toegang.");
        }

        _unitOfWork.Vaults.Delete(vault);

        _unitOfWork.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = "Vault verwijderd",
            EntityName = nameof(Vault),
            EntityId = vault.Id,
            CreatedAt = DateTime.UtcNow
        });

        _unitOfWork.Save();
    }
}

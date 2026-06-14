using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;

namespace Application.Interfaces;

public interface IVaultService
{
    IEnumerable<Vault> GetVaultsForUser(string userId);

    Vault? GetVaultForUser(int id, string userId);

    void CreateVault(string name, string? description, string userId, bool isPremiumUser);

    void UpdateVault(int id, string name, string? description, string userId);

    void DeleteVault(int id, string userId);
}

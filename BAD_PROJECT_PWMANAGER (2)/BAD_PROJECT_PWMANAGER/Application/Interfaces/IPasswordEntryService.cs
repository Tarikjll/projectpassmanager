using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;

namespace Application.Interfaces;

public interface IPasswordEntryService
{
    IEnumerable<PasswordEntry> GetEntriesForVault(int vaultId, string userId);
    IEnumerable<PasswordHistory> GetHistoryForEntry(int passwordEntryId, string userId);
    IEnumerable<PasswordEntry> GetEntriesForUser(string userId);

    IEnumerable<PasswordExport> GetExportsForEntry(int passwordEntryId, string userId);

    PasswordEntry? GetEntryForUser(int id, string userId);

    void CreateEntry(
        int vaultId,
        string platform,
        string username,
        string? url,
        string plainPassword,
        string? notes,
        string userId,
        bool isPremiumUser);

    void UpdateEntry(
        int id,
        string platform,
        string username,
        string? url,
        string plainPassword,
        string? notes,
        string userId,
        bool isPremiumUser);

    void DeleteEntry(int id, string userId);

    void RecordExport(int passwordEntryId, string userId, string destinationType, string destinationMasked);
}

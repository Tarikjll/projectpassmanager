using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;

namespace Application.Interfaces
{
    public interface IUnitOfWork
    {
        IRepository<Vault> Vaults { get; }
        IRepository<PasswordEntry> PasswordEntries { get; }
        IRepository<PasswordHistory> PasswordHistories { get; }
        IRepository<AuditLog> AuditLogs { get; }
        IRepository<PasswordExport> PasswordExports { get; }
        void Save();

    }
}

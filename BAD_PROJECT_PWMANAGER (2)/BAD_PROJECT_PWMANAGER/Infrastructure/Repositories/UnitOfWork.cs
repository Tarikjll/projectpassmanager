using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;



namespace Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;

        Vaults = new Repository<Vault>(_context);
        PasswordEntries = new Repository<PasswordEntry>(_context);
        PasswordHistories = new Repository<PasswordHistory>(_context);
        PasswordExports = new Repository<PasswordExport>(_context);
        AuditLogs = new Repository<AuditLog>(_context);
    }

    public IRepository<Vault> Vaults { get; }

    public IRepository<PasswordEntry> PasswordEntries { get; }

    public IRepository<PasswordHistory> PasswordHistories { get; }

    public IRepository<PasswordExport> PasswordExports { get; }

    public IRepository<AuditLog> AuditLogs { get; }

    public void Save()
    {
        _context.SaveChanges();
    }
}
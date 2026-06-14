using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;


namespace Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Vault> Vaults { get; set; }
    public DbSet<PasswordEntry> PasswordEntries { get; set; }
    public DbSet<PasswordHistory> PasswordHistories { get; set; }
    public DbSet<PasswordExport> PasswordExports { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<LoginAttempt> LoginAttempts { get; set; }
}

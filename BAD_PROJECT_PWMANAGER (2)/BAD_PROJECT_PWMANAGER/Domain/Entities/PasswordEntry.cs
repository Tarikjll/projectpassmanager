using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class PasswordEntry : BaseEntity
    {
        public int VaultId { get; set; }

        public Vault Vault { get; set; } = null!;

        public string Platform { get; set; } = string.Empty;

        public string Username { get; set; } = string.Empty;

        public string? Url { get; set; }

        public string EncryptedPassword { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public string? Notes { get; set; }

        public int StrengthScore { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public ICollection<PasswordHistory> PasswordHistories { get; set; } = new List<PasswordHistory>();

        public ICollection<PasswordExport> PasswordShares { get; set; } = new List<PasswordExport>();
    }
}

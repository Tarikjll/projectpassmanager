using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
   

    public class PasswordHistory : BaseEntity
    {
        public int PasswordEntryId { get; set; }

        public PasswordEntry PasswordEntry { get; set; } = null!;

        public string OldEncryptedPassword { get; set; } = string.Empty;

        public string OldPasswordHash { get; set; } = string.Empty;

        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    }
}

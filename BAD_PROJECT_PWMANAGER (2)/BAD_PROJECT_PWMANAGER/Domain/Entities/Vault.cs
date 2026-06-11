using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Domain.Entities
{
    public class Vault: BaseEntity
    {
       public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        public string UserId { get; set; } = string.Empty;  

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<PasswordEntry> PasswordEntries { get; set; } = new List<PasswordEntry>();


    }
}

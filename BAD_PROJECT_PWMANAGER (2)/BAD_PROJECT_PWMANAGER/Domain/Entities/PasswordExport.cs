using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Domain.Entities;

public class PasswordExport : BaseEntity
{
    public int PasswordEntryId { get; set; }

    public PasswordEntry PasswordEntry { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;

    public string DestinationType { get; set; } = string.Empty;
 

    public string DestinationMasked { get; set; } = string.Empty;
 

    public DateTime ExportedAt { get; set; } = DateTime.UtcNow;
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace Domain.Entities
{
    public class ApplicationUser : IdentityUser
    {

        [PersonalData]
        public string FirstName { get; set; } = string.Empty;
        
        [PersonalData]
        public string LastName { get; set; } = string.Empty;

        public string? ProfileImagePath { get; set; }

        public byte[]? ProfileImageData { get; set; }

        public string? ProfileImageContentType { get; set; }
    }
}
using System;
using System.Collections.Generic;

namespace BAD_PROJECT_PWMANAGER.ViewModels.Admin
{
    public class UserListViewModel
    {
        public string UserId { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public IList<string> Roles { get; set; } = new List<string>();

        public bool IsBanned { get; set; }

        public DateTimeOffset? BannedUntilUtc { get; set; }

        public string? BanReason { get; set; }
    }
}

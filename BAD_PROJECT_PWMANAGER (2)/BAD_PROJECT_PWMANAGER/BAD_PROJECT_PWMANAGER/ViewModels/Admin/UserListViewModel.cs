namespace BAD_PROJECT_PWMANAGER.ViewModels.Admin
{

    public class UserListViewModel
    {
        public string UserId { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public IList<string> Roles { get; set; } = new List<string>();
    }
}

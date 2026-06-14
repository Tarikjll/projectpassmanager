namespace BAD_PROJECT_PWMANAGER.ViewModels.Admin
{
    public class AuditLogListItemViewModel
    {
        public int Id { get; set; }

        public string UserName { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;


        public string UserEmail { get; set; } = "Onbekende gebruiker";

        public string Action { get; set; } = string.Empty;

        public string OperationLabel { get; set; } = string.Empty;

        public string EntityName { get; set; } = string.Empty;

        public int? EntityId { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}

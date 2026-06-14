namespace BAD_PROJECT_PWMANAGER.ViewModels.Admin
{
    public class AuditLogIndexViewModel
    {
        public IEnumerable<AuditLogListItemViewModel> Logs { get; set; } = [];

        public string SearchTerm { get; set; } = string.Empty;

        public string EntityFilter { get; set; } = "all";

        public string OperationFilter { get; set; } = "all";

    public int TotalCount { get; set; }

    public int FilteredCount { get; set; }

    public int CurrentPage { get; set; } = 1;

    public int TotalPages { get; set; } = 1;

    public int PageSize { get; set; } = 25;

    public bool HasFilters => !string.Equals(EntityFilter, "all", StringComparison.OrdinalIgnoreCase)
        || !string.Equals(OperationFilter, "all", StringComparison.OrdinalIgnoreCase)
        || !string.IsNullOrWhiteSpace(SearchTerm);

    public bool HasPagination => TotalPages > 1;
}
}

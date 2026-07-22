namespace CongoTravel.Models.DTOs
{
    public class AddRolesResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int RolesAdded { get; set; }
        public int TotalRoles { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<RoleOperationResult> SuccessRoles { get; set; } = new List<RoleOperationResult>();
        public List<RoleOperationResult> FailedRoles { get; set; } = new List<RoleOperationResult>();
    }

    public class RoleOperationResult
    {
        public string RoleAgent { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
        public bool Success { get; set; }
        public string? Error { get; set; }
        public string? Message { get; set; }
    }
}


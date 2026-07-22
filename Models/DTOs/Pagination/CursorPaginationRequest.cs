namespace CongoTravel.Models.DTOs.Pagination
{
    public class CursorPaginationRequest
    {
        public int PageSize { get; set; } = 20;
        public string? Cursor { get; set; }
    }
}


namespace CongoTravel.Models.DTOs.Pagination
{
    public class CursorPaginatedResult<T>
    {
        public List<T> Data { get; set; } = new List<T>();
        public string? NextCursor { get; set; }
        public bool HasMore { get; set; }
        public int Count => Data.Count;

        public CursorPaginatedResult(List<T> data, string? nextCursor, bool hasMore)
        {
            Data = data;
            NextCursor = nextCursor;
            HasMore = hasMore;
        }
    }
}


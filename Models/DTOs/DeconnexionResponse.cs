namespace CongoTravel.Models.DTOs
{
    public class DeconnexionResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int DevicesDesactives { get; set; }
    }
}


namespace CongoTravel.Models.DTOs
{
    public class DeconnexionRequest
    {
        public bool SupprimerTousLesDevices { get; set; } = false;
        public string? DeviceId { get; set; }
        public int? IdUserDevice { get; set; }
        public string? FcmToken { get; set; }
    }
}


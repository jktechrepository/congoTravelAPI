using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs
{
    public class UpdateUserDeviceDto
    {
        [Required]
        public int IdUserDevice { get; set; }
        
        [StringLength(500)]
        public string? FcmToken { get; set; }
        
        [StringLength(100)]
        public string? DeviceType { get; set; }
        
        [StringLength(100)]
        public string? DeviceModel { get; set; }
        
        [StringLength(50)]
        public string? OsVersion { get; set; }
        
        public bool? Statut { get; set; } = true;
    }
}

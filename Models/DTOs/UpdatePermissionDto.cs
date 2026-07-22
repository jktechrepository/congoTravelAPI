using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs
{
    public class UpdatePermissionDto
    {
        [Required]
        public int IdPermission { get; set; }
        
        [Required]
        [StringLength(100)]
        public string? Nom { get; set; }
        
        [StringLength(255)]
        public string? Description { get; set; }
        
        [StringLength(50)]
        public string? Categorie { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace TrendClothing.Models
{
    public class HeroImage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string ImageUrl { get; set; }
    }
}

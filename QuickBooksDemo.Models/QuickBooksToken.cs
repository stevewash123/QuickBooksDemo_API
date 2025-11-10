using System.ComponentModel.DataAnnotations;

namespace QuickBooksDemo.Models
{
    public class QuickBooksToken
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string RealmId { get; set; } = string.Empty;

        [Required]
        public string AccessToken { get; set; } = string.Empty;

        [Required]
        public string RefreshToken { get; set; } = string.Empty;

        [Required]
        public DateTime AccessTokenExpiry { get; set; }

        [Required]
        public DateTime RefreshTokenExpiry { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        // Only allow one active token set
        public bool IsActive { get; set; } = true;
    }
}
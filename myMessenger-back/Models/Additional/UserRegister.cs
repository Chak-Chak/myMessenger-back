using System.ComponentModel.DataAnnotations;

namespace myMessenger_back.Models.Additional
{
    public class UserRegister
    {
        [Required]
        [MinLength(2)]
        [MaxLength(20)]
        public string Name { get; set; }
        [Required]
        [EmailAddress]
        [StringLength(50)]
        public string Email { get; set; }
        [Required]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)\S{8,25}$", ErrorMessage = "Password length must be from 8 to 25, and at least 1 capital letter")]
        public string Password { get; set; }
    }
}

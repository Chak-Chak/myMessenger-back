using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace myMessenger_back.Models
{
    [Table("users")]
    public class User
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        [Column("name")]
        public string Name { get; set; }
        [Column("email")]
        public string Email { get; set; }
        [Column("password_hash")]
        public byte[] PasswordHash { get; set; }
        [Column("password_salt")]
        public byte[] PasswordSalt { get; set; }
        [Column("created_on")]
        public DateTime CreatedOn { get; set; }
        [Column("role")]
        public string Role { get; set; }
        [Column("refresh_token")]
        public string? RefreshToken { get; set; }
        [Column("refresh_token_created_on")]
        public DateTime? RefreshTokenCreatedOn { get; set; }
        [Column("refresh_token_expires_on")]
        public DateTime? RefreshTokenExpiresOn { get; set; }

    }
}

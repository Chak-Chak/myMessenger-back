using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace myMessenger_back.Models
{
    [Table("users_info")]
    public class User
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        [Column("name")]
        public string Name { get; set; }
        [Column("email")]
        public string Email { get; set; }
        [Column("password")]
        public string Password { get; set; }
        
        [Column("created_on")]
        public DateTime CreatedOn { get; set; }
    }
}

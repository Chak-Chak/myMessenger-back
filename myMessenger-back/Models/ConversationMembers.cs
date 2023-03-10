using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace myMessenger_back.Models
{
    [Table("conversation_members")]
    public class ConversationMembers
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        [Column("user_id")]
        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public UserData UserData { get; set; }
        [Column("conversation_id")]
        public int ConversationId { get; set; }
        [ForeignKey("ConversationId")]
        public Conversation ConversationData { get; set; }
    }
}

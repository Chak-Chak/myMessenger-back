using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using myMessenger_back.Dtos;

namespace myMessenger_back.Models
{
    [Table("conversations")]
    public class Conversation
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        [Column("name")]
        public string? Name { get; set; }

        [Column("is_direct_message")]
        public bool IsDirectMessage { get; set; }
        [Column("creator_user_id")]
        public int? CreatorUserId { get; set; }
        [ForeignKey("CreatorUserId")]
        public UserData CreatorUserData { get; set; }
        [Column("last_message_id")]
        public int? LastMessageId { get; set; }
        [ForeignKey("LastMessageId")]
        public Message LastMessageData { get; set; }
        public List<ConversationMembers> ConversationMembers { get; set; }
    }
}

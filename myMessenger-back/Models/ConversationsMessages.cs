using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace myMessenger_back.Models
{
    [Table("conversations_messages")]
    public class ConversationsMessages
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        [Column("conversation_id")]
        public int ConversationId { get; set; }
        [ForeignKey("ConversationId")]
        public Conversation ConversationData { get; set; }
        [Column("message_id")]
        public int MessageId { get; set; }
        [ForeignKey("MessageId")]
        public Message MessageData { get; set; }
    }
}

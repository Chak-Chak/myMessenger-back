using System.ComponentModel.DataAnnotations;

namespace myMessenger_back.Models.Additional
{
    public class UserSendMessage
    {
        [Required]
        [MinLength(1)]
        [MaxLength(500)]
        public string MessageText { get; set; }
        [Required]
        public int ConversationId { get; set; }
    }
}

using myMessenger_back.Models;

namespace myMessenger_back.Dtos
{
    public class ConversationDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsDirectMessage { get; set; }
        public int? CreatorUserId { get; set; }
        public UserDataDto CreatorUserData { get; set; }
        public int? LastMessageId { get; set; }
        public MessageDto LastMessageData { get; set; }
        public virtual List<ConversationMembers> ConversationMembers { get; set; }
    }
}

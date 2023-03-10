using myMessenger_back.Dtos;
using myMessenger_back.Models;

namespace myMessenger_back
{
    public static class Extensions
    {
        public static UserDataDto AsDto(this UserData user)
        {
            return new UserDataDto()
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                CreatedOn = user.CreatedOn,
            };
        }
        public static ConversationDto AsDto(this Conversation conversation/*, Message lastMessage*/)
        {
            return new ConversationDto()
            {
                Id = conversation.Id,
                Name = conversation.Name,
                IsDirectMessage = conversation.IsDirectMessage,
                CreatorUserId = conversation.CreatorUserId,
                CreatorUserData = conversation.CreatorUserData?.AsDto(),
                LastMessageId = conversation.LastMessageId,
                LastMessageData = conversation.LastMessageData?.AsDto(),
                ConversationMembers = conversation.ConversationMembers,
                //LastMessageData = lastMessage.AsDto(),
            };
        }
        public static MessageDto AsDto(this Message message)
        {
            return new MessageDto()
            {
                Id = message.Id,
                SenderId = message.SenderId,
                SenderUserData = message.SenderUserData?.AsDto(),
                MessageText = message.MessageText,
                IsReading = message.IsReading,
                SendingDate = message.SendingDate,
            };
        }
    }
}

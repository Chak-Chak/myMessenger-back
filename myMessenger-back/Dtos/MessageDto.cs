using myMessenger_back.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace myMessenger_back.Dtos
{
    public class MessageDto
    {
        public int Id { get; set; }
        public int SenderId { get; set; }
        public UserDataDto SenderUserData { get; set; }
        public string MessageText { get; set; }
        public bool IsReading { get; set; }
        public DateTime? SendingDate { get; set; }
    }
}

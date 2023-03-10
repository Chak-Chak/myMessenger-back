using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using myMessenger_back.Dtos;

namespace myMessenger_back.Models
{
    [Table("messages")]
    public class Message
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("sender_id")]
        public int SenderId { get; set; }
        [ForeignKey("SenderId")]
        public UserData SenderUserData { get; set; }
        [Column("message_text")]
        public string MessageText { get; set; }
        [Column("is_reading")]
        public bool IsReading { get; set; }
        [Column("sending_date")]
        public DateTime? SendingDate { get; set; }
    }
}

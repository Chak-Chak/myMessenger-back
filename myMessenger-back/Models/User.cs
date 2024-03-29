﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace myMessenger_back.Models
{
    [Table("users")]
    public class User
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public UserData UserData { get; set; }
    }
}

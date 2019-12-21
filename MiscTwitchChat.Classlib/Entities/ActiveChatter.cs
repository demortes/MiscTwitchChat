using System;
using System.ComponentModel.DataAnnotations;

namespace MiscTwitchChat.Classlib.Entities
{
    public class ActiveChatter
    {
        [Key]
        public int Id { get; set; }
        public string Username { get; set; }
        public string Channel { get; set; }
        public DateTimeOffset LastSeen { get; set; }
        public DateTimeOffset FirstSeen { get; set; } = DateTimeOffset.UtcNow;
    }
}
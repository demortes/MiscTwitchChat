using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MiscTwitchChat.Classlib.Entities
{
    public class Disconsenter
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Name { get; set; }
        [Required]
        public DateTimeOffset Created { get; set; } = DateTimeOffset.UtcNow;
    }
}
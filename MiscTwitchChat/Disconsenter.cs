using System.ComponentModel.DataAnnotations;

namespace MiscTwitchChat
{
    public class Disconsenter
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
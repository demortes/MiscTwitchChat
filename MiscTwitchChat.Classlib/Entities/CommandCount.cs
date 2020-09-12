using System.ComponentModel.DataAnnotations;

namespace MiscTwitchChat.Classlib.Entities
{
    public class CommandCount
    {
        [Required]
        public string Channel { get; set; } //This is going to be the name of the server in discord.

        [Required]
        public string TargetUser { get; set; }

        [Required]
        public string CommandUsed { get; set; }
        public int Count { get; set; } = 0;
    }
}

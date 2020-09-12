namespace MiscTwitchChat.Models
{
    public class TwitchChattersDTO
    {
        public _Links _links { get; set; }
        public int chatter_count { get; set; }
        public Chatters chatters { get; set; }

        public class _Links
        {
        }

        public class Chatters
        {
            public string[] broadcaster { get; set; }
            public string[] vips { get; set; }
            public string[] moderators { get; set; }
            public string[] staff { get; set; }
            public string[] admins { get; set; }
            public string[] global_mods { get; set; }
            public string[] viewers { get; set; }
        }

    }
}

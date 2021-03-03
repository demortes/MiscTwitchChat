using Newtonsoft.Json;

namespace MiscTwitchChat.Models
{
    public class DogResponse
    {
        [JsonProperty("fact")]
        public string Fact { get; set; }
    }

}

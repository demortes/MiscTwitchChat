using MiscTwitchChat.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace MiscTwitchChat.Helpers
{
    public class TwitchApiClasslib
    {
        public static async Task<string> GetRandomConsentingChatter(MiscTwitchDbContext _db, string channel, string origUser, string command = null, bool? increaseCount = null)
        {
            string target = "No one";
            do
            {
                //Pull viewers from twitch api.
                var url = $"http://tmi.twitch.tv/group/user/{channel.ToLower()}/chatters";
                using HttpClient client = new HttpClient();
                var response = await client.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();
                var serialized = JsonConvert.DeserializeObject<TwitchChattersDTO>(content);
                var allChatters = new List<string>();
                var chatters = serialized.chatters;
                allChatters.AddRange(chatters.admins);
                allChatters.AddRange(chatters.broadcaster);
                allChatters.AddRange(chatters.global_mods);
                allChatters.AddRange(chatters.moderators);
                allChatters.AddRange(chatters.staff);
                allChatters.AddRange(chatters.viewers);
                allChatters.AddRange(chatters.vips);
                //Remove disconsentors.
                allChatters = allChatters.Distinct().ToList();
                foreach(var disconsentor in _db.Disconsenters.Select(p => p.Name))
                {
                    allChatters.Remove(disconsentor);
                }

                allChatters.Remove(origUser);
                //Pick one randomly.
                target = allChatters[new Random().Next(0, allChatters.Count - 1)];
            } while (target == origUser && _db.Disconsenters.FirstOrDefault(p => p.Name == target) != null);

            if (increaseCount != null && increaseCount == true)
            {
                //TODO: Get any DB entry for command @ channel, if none, create one. If any, increase count.
            }
            return target;
        }
    }
}

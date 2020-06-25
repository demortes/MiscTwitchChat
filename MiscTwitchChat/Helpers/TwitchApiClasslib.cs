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
        public static string GetRandomConsentingChatter(MiscTwitchDbContext db, string channel, string origUser, string command = null, bool? increaseCount = null)
        {
            string target = "No one";
            do
            {
                //Pull viewers from twitch api.
                var allChatters = db.ActiveChatters
                    .Where(p => p.Channel == channel && p.LastSeen >= DateTimeOffset.UtcNow.AddMinutes(-60))
                    .Select(p => p.Username)
                    .Distinct().ToList();
                foreach (var disconsentor in db.Disconsenters.Select(p => p.Name))
                {
                    allChatters.Remove(disconsentor);
                }

                allChatters.Remove(origUser);
                //Pick one randomly.
                target = allChatters[new Random().Next(0, allChatters.Count - 1)];
            } while (target == origUser && db.Disconsenters.FirstOrDefault(p => p.Name == target) != null);

            if (increaseCount != null && increaseCount == true)
            {
                //TODO: Get any DB entry for command @ channel, if none, create one. If any, increase count.
            }
            return target;
        }
    }
}

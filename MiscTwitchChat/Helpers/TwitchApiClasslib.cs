using System;
using System.Collections.Generic;
using System.Linq;

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

            return target;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MiscTwitchChat.Models;
using Newtonsoft.Json;

namespace MiscTwitchChat.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SpankController : ControllerBase
    {
        private MiscTwitchDbContext _db;
        private ILogger _logger;

        public SpankController(ILogger<SpankController> logger, MiscTwitchDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        [HttpGet("{channel}/{origUser}")]
        public async Task<string> SpankAsync(string channel, string origUser)
        {
            _logger.LogInformation($"Starting spank from {origUser} in {channel}");
            string target = "No one";
            do
            {
                //Pull viewers from twitch api.
                var url = $"http://tmi.twitch.tv/group/user/{channel.ToLower()}/chatters";
                HttpClient client = new HttpClient();
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

                //Pick one randomly.
                target = allChatters[new Random().Next(0, allChatters.Count - 1)];
            } while (_db.Disconsenters.FirstOrDefault(p => p.Name == target) != default);

            return target;
        }

        [HttpGet("{channel}/{origUser}/consent")]
        public async Task<string> UpdateConsent(string channel, string origUser)
        {
            string rval = string.Empty;
            var disconsenter = _db.Disconsenters.FirstOrDefault(p => p.Name == origUser);
            if (disconsenter == null)
            {
                _db.Disconsenters.Add(new Disconsenter
                {
                    Name = origUser
                });
                rval = $"{origUser} has registered they do not consent.";
            }
            else
            {
                _db.Disconsenters.Remove(disconsenter);
                rval = $"{origUser} has registered that they DO consent.";
            }

            await _db.SaveChangesAsync();
            return rval;
        }
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiscTwitchChat.Classlib.Entities;
using System.Linq;
using System.Security.Claims;

namespace MiscTwitchChat.Controllers
{
    [Authorize]
    [Route("[controller]/[action]")]
    public class ActivityBotController : Controller
    {
        private readonly MiscTwitchDbContext _db;

        public ActivityBotController(MiscTwitchDbContext db)
        {
            _db = db;
        }

        // GET: /ActivityBot/GetOptInStatus
        [HttpGet]
        public IActionResult GetOptInStatus()
        {
            var channel = User.FindFirstValue(ClaimTypes.Name); // Twitch username
            if (string.IsNullOrEmpty(channel))
                return Unauthorized();

            var setting = _db.Settings.FirstOrDefault(s => s.Channel == channel && s.Name == "ActivityBotOptIn");
            bool optedIn = setting != null && setting.Value == "true";
            return Json(new { optedIn });
        }

        // POST: /ActivityBot/SetOptInStatus
        [HttpPost]
        public IActionResult SetOptInStatus([FromForm] bool optIn)
        {
            var channel = User.FindFirstValue(ClaimTypes.Name); // Twitch username
            if (string.IsNullOrEmpty(channel))
                return Unauthorized();

            var setting = _db.Settings.FirstOrDefault(s => s.Channel == channel && s.Name == "ActivityBotOptIn");
            if (setting == null)
            {
                setting = new Setting
                {
                    Channel = channel,
                    Name = "ActivityBotOptIn",
                    Value = optIn ? "true" : "false"
                };
                _db.Settings.Add(setting);
            }
            else
            {
                setting.Value = optIn ? "true" : "false";
                _db.Settings.Update(setting);
            }
            _db.SaveChanges();
            return Ok();
        }
    }
}

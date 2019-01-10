using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace MiscTwitchChat.Controllers
{
    /// <summary>
    /// Get your eight ball here.
    /// </summary>
    public class EightBallController : ApiController
    {
        private string[] response =
        {
            "Yes, of course!",
            "Without a doubt, yes.",
            "You can count on it.",
            "For sure!",
            "Ask me later.",
            "I'm not sure.",
            "I can't tell you right now.",
            "I'll tell you after my nap.",
            "No way!",
            "Doesn't look great.",
            "I don't think so.",
            "Without a doubt, no.",
            "The answer is clearly NO!",
            "Today doesn't look good. Tomorrow doesn't either. Nor the year for that fact.",
            "Only I will ever know",
            "I better not say.",
            "Repeat after me... I am not that lucky."
        };

        public HttpResponseMessage Get()
        {
            var r = response[new Random().Next(0, response.Length - 1)];
            HttpResponseMessage resp = new HttpResponseMessage()
            {
                Content = new StringContent(r)
            };
            return resp;
        }
    }
}

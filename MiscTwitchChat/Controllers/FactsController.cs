using Microsoft.AspNetCore.Mvc;
using MiscTwitchChat.Helpers;
using System;

namespace MiscTwitchChat.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FactsController : ControllerBase
    {
        private readonly StJude _facts;

        public FactsController(StJude facts)
        {
            _facts = facts;
        }

        private readonly Random _random = new();

        [HttpGet]
        public string Get()
        {
            var rval = string.Empty;
            var randCount = _facts.Facts.Length-1;
            rval = _facts.Facts[_random.Next(0, randCount)];
            return rval;
        }
    }
}

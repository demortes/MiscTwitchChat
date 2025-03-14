﻿using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace MiscTwitchChat.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DadController : ControllerBase
    {
        [HttpGet]
        public async Task<string> GetDadJoke()
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("text/plain"));
            var resp = await client.GetAsync("https://icanhazdadjoke.com");
            var joke = await resp.Content.ReadAsStringAsync();
            return joke;
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MiscTwitchChat.Models;
using Newtonsoft.Json;

namespace MiscTwitchChat.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PickupController : ControllerBase
    {
        private string endpoint = "http://pebble-pickup.herokuapp.com/tweets/random";
        [HttpGet]
        public async System.Threading.Tasks.Task<string> GetAsync()
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("text/plain"));
            var response = await client.GetAsync(endpoint);
            var contentReader = new StreamReader(await response.Content.ReadAsStreamAsync());
            JsonSerializer serializer = new JsonSerializer();
            var dogResponse = (PickupResponse)serializer.Deserialize(contentReader, typeof(PickupResponse));
            return dogResponse.tweet;
        }
    }
}
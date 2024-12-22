using Microsoft.AspNetCore.Mvc;
using MiscTwitchChat.Models;
using Newtonsoft.Json;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;

namespace MiscTwitchChat.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CatController : ControllerBase
    {
        private string endpoint = "https://catfact.ninja/fact";
        [HttpGet]
        public async System.Threading.Tasks.Task<string> GetAsync()
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("text/plain"));
            var response = await client.GetAsync(endpoint);
            var contentReader = new StreamReader(await response.Content.ReadAsStreamAsync());
            JsonSerializer serializer = new JsonSerializer();
            var catResponse = (CatResponse)serializer.Deserialize(contentReader, typeof(CatResponse));
            return catResponse.fact;
        }
    }
}
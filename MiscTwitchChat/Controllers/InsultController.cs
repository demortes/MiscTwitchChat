using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace MiscTwitchChat.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InsultController : ControllerBase
    {
        [HttpGet]
        public async Task<string> InsultAsync(string channel, string user, string target = null)
        {
            var url = "https://insult.mattbas.org/api/insult.txt";
            var client = new HttpClient();
            if(!string.IsNullOrWhiteSpace(target))
            {
                url += $"?who={target}";
            }
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
    }
}
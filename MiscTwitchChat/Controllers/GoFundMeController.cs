using Microsoft.AspNetCore.Mvc;
using MiscTwitchChat.Classlib.Entities;
using MiscTwitchChat.ClassLib;
using System.Threading.Tasks;

namespace MiscTwitchChat.Controllers
{
    /// <summary>
    /// Uses the GoFundMe GraphQL to extract donation information according to the query in the URL. Can be used
    /// to trigger sounds accordingly. Just remember to pass in Cursor to prevent duplicates.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class GoFundMeController : ControllerBase
    {
        private IGoFundMeService _goFundMeService;
        public GoFundMeController(IGoFundMeService goFundMeService)
        {
            _goFundMeService = goFundMeService;
        }

        [HttpGet]
        public Task<Donations> GetDonations(string slug, string? cursor, int? first, int? last)
        {
            return _goFundMeService.GetDonations(slug, cursor, first, last);
        }
    }
}

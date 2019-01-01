using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace MiscTwitchChat.Controllers
{
    public class UrbanController : ApiController
    {
        // GET api/values

        // GET api/values/5

        public HttpResponseMessage Get(string term)
        {
            try
            {
                //Look it up.
                var result = UrbanDictionaryNet.UrbanDictionary.Define(term);

                if (result.Definitions.Count < 1)
                {
                    var res = new HttpResponseMessage(HttpStatusCode.NotFound)
                    {
                        Content = new StringContent("404: " + term + " not found.")
                    };
                    return res;
                }
                //Pass it back in formatted string, truncated.
                var fullResult = result.Definitions.OrderByDescending(p => p.CurrentVote).First().Definition.Replace("\r\n", " ");
                var truncatedResult = fullResult.Substring(0, fullResult.Length < 252 ? fullResult.Length : 252) + (fullResult.Length > 252 ? "..." : "");

                HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
                response.Content = new StringContent(truncatedResult);

                return response;
            }
            catch
            {
                var res = new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent("ABORT ABORT ABORT: Server error. Something broke, but not likely your bot.")
                };
                return res;
            }
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using MiscTwitchChat.Models;
using System.Diagnostics;

namespace MiscTwitchChat.Controllers
{
    public class HomeController : Controller
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HomeController"/> class.
        /// </summary>
        public HomeController()
        {

        }
        /// <summary>
        /// Displays the home page.
        /// </summary>
        /// <summary>
        /// Displays the application's home page.
        /// </summary>
        /// <returns>The view for the home page.</returns>
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Displays the about page.
        /// </summary>
        /// <summary>
        /// Displays the About page.
        /// </summary>
        /// <returns>The view for the About page.</returns>
        public IActionResult About()
        {
            return View();
        }

        /// <summary>
        /// Displays the error page.
        /// </summary>
        /// <summary>
        /// Displays the error page populated with the current request identifier.
        /// </summary>
        /// <returns>The error page view populated with an <see cref="ErrorViewModel"/> containing the current request identifier.</returns>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

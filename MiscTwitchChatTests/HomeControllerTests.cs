using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiscTwitchChat.Controllers;

namespace MiscTwitchChatTests
{
    [TestClass]
    public class HomeControllerTests
    {
        [TestMethod]
        public async System.Threading.Tasks.Task TestingIndexAsync()
        {
            var uut = new HomeController();
            var result = uut.Index();

        }
    }
}

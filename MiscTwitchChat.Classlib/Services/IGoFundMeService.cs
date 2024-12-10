using MiscTwitchChat.Classlib.Entities;
using System.Threading.Tasks;

namespace MiscTwitchChat.ClassLib
{
    public interface IGoFundMeService
    {
        Task<Donations> GetDonations(string slug, string? cursor, int? first, int? last);
    }
}
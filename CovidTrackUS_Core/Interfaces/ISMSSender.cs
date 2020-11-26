using System.Threading.Tasks;

namespace CovidTrackUS_Core.Interfaces
{
    public interface ISMSSender
    {
        Task<bool> SendMessageAsync(string toPhoneNumber,string fromNumber, string txt);
    }
}

using System.Globalization;
using System.Threading.Tasks;

namespace ExtendedMasterDetailPage.Services
{
    public interface ILocalizeService
    {
        bool IsRightToLeft { get; }

        CultureInfo GetCurrentCultureInfo();

        Task<CultureInfo> SetLocale();
    }
}
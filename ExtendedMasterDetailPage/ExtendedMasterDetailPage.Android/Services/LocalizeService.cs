using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using Android.Net;
using Android.Provider;
using Java.Util;
using Xamarin.Forms;
using ExtendedMasterDetailPage.Services;
using ExtendedMasterDetailPage.Droid.Services;

[assembly: Xamarin.Forms.Dependency(typeof(LocalizeService))]
namespace ExtendedMasterDetailPage.Droid.Services
{
    public class LocalizeService : ILocalizeService
    {
        private BootCompletedBroadcastMessageReceiver _br;

        public CultureInfo GetCurrentCultureInfo()
        {
            var androidLocale = Locale.Default;
            var netLanguage = androidLocale.ToString().Replace("_", "-").Replace("iw", "he");
            return new CultureInfo(netLanguage);
        }

        public async Task<CultureInfo> SetLocale()
        {
            _br = new BootCompletedBroadcastMessageReceiver();
            Forms.Context.RegisterReceiver(_br, new IntentFilter(Intent.ActionLocaleChanged));

            var ci = GetCurrentCultureInfo();

            var activity = Forms.Context as MainActivity;
            if (activity != null)
            {
                var intent = activity.Intent;
                Forms.Context.StartActivity(new Intent(Settings.ActionLocaleSettings));

                while (!_br.IsComplited)
                    await Task.Delay(500);

                _br.Reset();
                activity.Finish();
                activity.StartActivity(intent);
            }

            return ci;
        }

        public bool IsRightToLeft => GetCurrentCultureInfo().TextInfo.IsRightToLeft;
    }

    public class BootCompletedBroadcastMessageReceiver : BroadcastReceiver
    {
        public BootCompletedBroadcastMessageReceiver()
        {
            IsComplited = false;
        }

        public bool IsComplited { get; private set; }

        public Uri Uri { get; private set; }

        public override void OnReceive(Context context, Intent intent)
        {
            IsComplited = true;

            var ci = DependencyService.Get<ILocalizeService>().GetCurrentCultureInfo();
            Thread.CurrentThread.CurrentCulture = ci;
            Thread.CurrentThread.CurrentUICulture = ci;
        }

        public void Reset()
        {
            IsComplited = false;
        }
    }
}
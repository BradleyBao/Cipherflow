using Microsoft.Windows.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLock.Functions
{
    public class SettingManager
    {
        // Fix: Replace 'ApplicationData.Current.LocalSettings' with 'ApplicationData.GetDefault().LocalSettings'
        private static readonly ApplicationDataContainer settings = ApplicationData.GetDefault().LocalSettings;

        public static string Language
        {
            get => settings.Values["language"] as string ?? "auto";
            set => settings.Values["language"] = value;
        }

        public static bool Password
        {
            get => settings.Values["password"] is bool password ? password : false;
            set => settings.Values["password"] = value;
        }

        public static bool MFA
        {
            get => settings.Values["MFAOn"] is bool mfa ? mfa : false;
            set => settings.Values["MFAOn"] = value;
        }

        public static bool WindowsHello
        {
            get => settings.Values["WindowsHello"] is bool mfa ? mfa : false;
            set => settings.Values["WindowsHello"] = value;
        }
    }
}

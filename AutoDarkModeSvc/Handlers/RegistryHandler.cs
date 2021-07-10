using Microsoft.Win32;
using System;
using System.Threading;
using WindowsInput;
using WindowsInput.Native;

namespace AutoDarkModeSvc.Handlers
{
    static class RegistryHandler
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Switches system applications theme
        /// </summary>
        /// <param name="theme">0 for dark, 1 for light theme</param>
        public static void SetAppsTheme(int theme)
        {
            using var key = GetPersonalizeKey();
            key.SetValue("AppsUseLightTheme", theme, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Switches the system theme
        /// </summary>
        /// <param name="theme"><0 for dark, 1 for light theme</param>
        public static void SetSystemTheme(int theme)
        {
            using var key = GetPersonalizeKey();
            key.SetValue("SystemUsesLightTheme", theme, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Sets the taskbar color prevalence
        /// </summary>
        /// <param name="theme">0 for disabled, 1 for enabled</param>
        public static void SetColorPrevalence(int theme)
        {
            using var key = GetPersonalizeKey();
            key.SetValue("ColorPrevalence", theme, RegistryValueKind.DWord);
        }

        /// <summary>
        /// Checks if color prevalence is enabled
        /// </summary>
        /// <returns>true if enabled; false otherwise</returns>
        public static bool IsColorPrevalence()
        {
            using var key = GetPersonalizeKey();
            var enabled = key.GetValue("ColorPrevalence").Equals(1);
            return enabled;
        }

        /// <summary>
        /// Checks if system apps theme is light
        /// </summary>
        /// <returns>true if light; false if dark</returns>
        public static bool AppsUseLightTheme()
        {
            using var key = GetPersonalizeKey();
            var enabled = key.GetValue("AppsUseLightTheme").Equals(1);
            return enabled;
        }

        /// <summary>
        /// Checks if the system's theme is light
        /// </summary>
        /// <returns>true if light; false if dark</returns>
        public static bool SystemUsesLightTheme()
        {
            using var key = GetPersonalizeKey();
            var enabled = key.GetValue("SystemUsesLightTheme").Equals(1);
            return enabled;
        }

        /// <summary>
        /// Retrieves the operating system version
        /// </summary>
        /// <returns>operating system version string</returns>
        public static string GetOSversion()
        {
            var osVersion = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ReleaseId", "").ToString();
            return osVersion;
        }

        /// <summary>
        /// Gets the current user's personaliation registry key
        /// </summary>
        /// <returns>HKCU personalization RegistryKey</returns>
        private static RegistryKey GetPersonalizeKey()
        {
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", true);
            return registryKey;
        }

        /// <summary>
        /// Adds the application to Windows autostart
        /// </summary>
        public static bool AddAutoStart()
        {
            try
            {
                RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
                registryKey.SetValue("AutoDarkMode", '\u0022' + Extensions.ExecutionPath + '\u0022');
                registryKey.Dispose();
                return true;
            } 
            catch (Exception ex)
            {
                Logger.Error(ex, "could not add AutoDarkModeSvc to autostart");
                return false;
            }

        }

        /// <summary>
        /// Removes the application from Windows autostart
        /// </summary>
        public static bool RemoveAutoStart()
        {
            try
            {
                using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
                registryKey.DeleteValue("AutoDarkMode", false);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "could not remove AutoDarkModeSvc from autostart");
            }
            return false;
        }
        /// <summary>
        /// Changes the office theme
        /// </summary>
        /// <param name="themeValue">0 = colorful, 3 = grey, 4 = black, 5 = white</param>
        public static void OfficeTheme(byte themeValue)
        {
            string officeCommonKey = @"Software\Microsoft\Office\16.0\Common";

            //edit first registry key
            using RegistryKey commonKey = Registry.CurrentUser.OpenSubKey(officeCommonKey, true);
            commonKey.SetValue("UI Theme", themeValue);

            //search for the second key and then change it
            using RegistryKey identityKey = Registry.CurrentUser.OpenSubKey(officeCommonKey + @"\Roaming\Identities\", true);

            string msaSubkey = @"\Settings\1186\{00000000-0000-0000-0000-000000000000}\";
            string anonymousSubKey = msaSubkey + @"\PendingChanges";

            foreach (var v in identityKey.GetSubKeyNames())
            {
                //registry key for users logged in with msa
                if (!v.Equals("Anonymous"))
                {
                    try
                    {
                        using RegistryKey settingsKey = identityKey.OpenSubKey(v + msaSubkey, true);
                        settingsKey.SetValue("Data", new byte[] { themeValue, 0, 0, 0 });
                    }
                    catch
                    {
                        using RegistryKey createdSettingsKey = identityKey.CreateSubKey(v + msaSubkey, true);
                        createdSettingsKey.SetValue("Data", new byte[] { themeValue, 0, 0, 0 });
                    }
                }
                //registry key for users without msa
                else
                {
                    try
                    {
                        using RegistryKey settingsKey = identityKey.OpenSubKey(v + anonymousSubKey, true);
                        settingsKey.SetValue("Data", new byte[] { themeValue, 0, 0, 0 });
                    }
                    catch
                    {
                        using RegistryKey createdSettingsKey = identityKey.CreateSubKey(v + anonymousSubKey, true);
                        createdSettingsKey.SetValue("Data", new byte[] { themeValue, 0, 0, 0 });
                    }
                }
            }
        }

        //Colour filter grayscale feature
        public static void ColorFilterKeySender(bool dark)
        {
            var filterKey = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\ColorFiltering", "Active", null);
            if (dark && filterKey.Equals(0) || !dark && filterKey.Equals(1))
            {
                //simulate key presses
                InputSimulator inputSimulator = new InputSimulator();
                inputSimulator.Keyboard.KeyDown(VirtualKeyCode.LWIN);
                inputSimulator.Keyboard.KeyDown(VirtualKeyCode.LCONTROL);
                inputSimulator.Keyboard.KeyPress(VirtualKeyCode.VK_C);
                inputSimulator.Keyboard.KeyUp(VirtualKeyCode.LWIN);
                inputSimulator.Keyboard.KeyUp(VirtualKeyCode.LCONTROL);
            }
        }
        public static void ColorFilterSetup()
        {
            var filterType = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\ColorFiltering", true);
            //on clean installs this registry key doesn't exist, so we need to create it
            if (filterType == null)
            {
                filterType.Dispose();
                filterType = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\ColorFiltering", true);
            }
            var currentValue = filterType.GetValue("Active", null);
            if (currentValue == null)
            {
                filterType.SetValue("Active", 0, RegistryValueKind.DWord);
            }

            //set filtertype to 0 for grayscale and activate hotkey functionality of windows
            filterType.SetValue("FilterType", 0, RegistryValueKind.DWord); // 0 = gray
            filterType.SetValue("HotkeyEnabled", 1, RegistryValueKind.DWord); //and we activate the hotkey as free bonus :)
            filterType.Dispose();
        }
    }
}
﻿using System;
using System.Collections.Generic;
using System.Text;
using AutoDarkModeSvc.Handlers;

namespace AutoDarkModeSvc.Config
{
    class RuntimeConfig
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private static RuntimeConfig rtc;
        public static RuntimeConfig Instance()
        {
            if (rtc == null)
            {
                rtc = new RuntimeConfig();
            }
            return rtc;
        }
        protected RuntimeConfig()
        {
            CurrentAppsTheme = RegistryHandler.AppsUseLightTheme() ? Theme.Light : Theme.Dark;
            CurrentSystemTheme = RegistryHandler.SystemUsesLightTheme() ? Theme.Light : Theme.Dark;

            try
            {
                CurrentEdgeTheme = RegistryHandler.EdgeUsesLightTheme() ? Theme.Light : Theme.Dark;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "could not retrieve edge theme key value");
                CurrentEdgeTheme = Theme.Undefined;
            }

            CurrentColorPrevalence = RegistryHandler.IsColorPrevalence();
            CurrentWallpaperTheme = Theme.Undefined;
            CurrentWindowsThemeName = ThemeHandler.GetCurrentThemeName();
            CurrentOfficeTheme = Theme.Undefined;
            ForcedTheme = Theme.Undefined;
            PostponeSwitch = false;
        }

        public Theme CurrentAppsTheme { get; set; }
        public Theme CurrentSystemTheme { get; set; }
        public Theme CurrentEdgeTheme { get; set; }
        public Theme CurrentWallpaperTheme { get; set; }
        public Theme CurrentOfficeTheme { get; set; }
        public bool ColorFilterEnabled { get; set; }
        public bool CurrentColorPrevalence { get; set; }
        public Theme ForcedTheme { get; set; }
        public string CurrentWindowsThemeName { get; set; }
        public bool PostponeSwitch { get; set; }
    }
}

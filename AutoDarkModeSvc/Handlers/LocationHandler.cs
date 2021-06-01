using System;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using AutoDarkModeSvc.Config;
using AutoDarkModeApp;

namespace AutoDarkModeSvc.Handlers
{
    static class LocationHandler
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        /// <summary>
        /// Refreshes sunrise and sunset based on latittude and longitude found in the configuration file.
        /// </summary>
        /// <param name="configBuilder">config builder for the AutoDarkModeConfig to allow saving</param>
        private static void UpdateSunTime(AdmConfigBuilder configBuilder)
        {
            int[] sun = SunDate.CalculateSunriseSunset(configBuilder.LocationData.Lat, configBuilder.LocationData.Lon);
            configBuilder.LocationData.Sunrise = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, sun[0] / 60, sun[0] - (sun[0] / 60) * 60, 0);
            configBuilder.LocationData.Sunset = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, sun[1] / 60, sun[1] - (sun[1] / 60) * 60, 0);
            Logger.Info($"new sunrise ({configBuilder.LocationData.Sunrise.ToString("HH:mm")}) and new sunset ({configBuilder.LocationData.Sunset.ToString("HH:mm")})");
        }

        /// <summary>
        /// Updates the user's geoposition (latitude and longitude) and save to the config
        /// </summary>
        /// <param name="configBuilder">config builder for the AutoDarkModeConfig</param>
        /// <returns></returns>
        public static async Task<bool> UpdateGeoposition(AdmConfigBuilder configBuilder)
        {
            var permission = await Geolocator.RequestAccessAsync();
            var success = false;

            void SetLatAndLon(BasicGeoposition position)
            {
                configBuilder.LocationData.Lon = position.Longitude;
                configBuilder.LocationData.Lat = position.Latitude;
                configBuilder.LocationData.LastUpdate = DateTime.Now;

                Logger.Info($"retrieved latitude {position.Latitude} and longitude {position.Longitude}");
                success = true;
            }

            switch (permission)
            {
                case GeolocationAccessStatus.Allowed:
                    Geolocator locator = new Geolocator();
                    Geoposition location = await locator.GetGeopositionAsync();
                    BasicGeoposition position = location.Coordinate.Point.Position;

                    SetLatAndLon(position);

                    break;
                default:
                    if (Geolocator.DefaultGeoposition.HasValue)
                    {
                        BasicGeoposition defaultPosition = Geolocator.DefaultGeoposition.Value;
                        SetLatAndLon(defaultPosition);
                    }
                    else
                    {
                        configBuilder.Config.Location.Enabled = false;
                        Logger.Warn($"no geolocation access, please enable in system settings");
                    }

                    break;
            }

            UpdateSunTime(configBuilder);

            try
            {
                configBuilder.SaveLocationData();
            }
            catch (Exception e)
            {
                Logger.Error(e, "could not update configuration file while retrieving location");
            }

            return success;
        }

        /// <summary>
        /// Calculate sundates based on a user configurable offset found in AutoDarkModeConfig. 
        /// Call this method to generate the final sunrise and sunset times if location based switching is enabled
        /// </summary>
        /// <param name="config">AutoDarkMoeConfig object</param>
        /// <param name="sunrise_out"></param>
        /// <param name="sunset_out"></param>
        public static void GetSunTimesWithOffset(AdmConfigBuilder builder, out DateTime sunrise_out, out DateTime sunset_out)
        {
            //Add offset to sunrise and sunset hours using Settings
            DateTime sunrise = builder.LocationData.Sunrise;
            sunrise = sunrise.AddMinutes(builder.Config.Location.SunriseOffsetMin);

            DateTime sunset = builder.LocationData.Sunset;
            sunset = sunset.AddMinutes(builder.Config.Location.SunsetOffsetMin);

            sunrise_out = sunrise;
            sunset_out = sunset;
        }
    }
}

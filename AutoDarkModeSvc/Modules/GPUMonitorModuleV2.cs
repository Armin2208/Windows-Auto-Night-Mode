﻿using AutoDarkModeSvc.Config;
using AutoDarkModeSvc.Handlers;
using AutoDarkModeSvc.Timers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace AutoDarkModeSvc.Modules
{
    class GPUMonitorModuleV2 : AutoDarkModeModule
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        //private static readonly string NoSwitch = "no_switch_pending";
        private static readonly string ThreshLow = "threshold_low";
        //private static readonly string ThreshBelow = "theshold_below";
        private static readonly string ThreshHigh = "threshold_high";
        //private static readonly string Frozen = "frozen";

        public override string TimerAffinity { get; } = TimerName.Main;
        private GlobalState State { get; }
        private AdmConfigBuilder ConfigBuilder { get; }
        private int Counter { get; set; }
        private bool PostponeLight { get; set; }
        private bool PostponeDark { get; set; }

        public GPUMonitorModuleV2(string name, bool fireOnRegistration) : base(name, fireOnRegistration)
        {
            State = GlobalState.Instance();
            ConfigBuilder = AdmConfigBuilder.Instance();
            PostponeDark = false;
            PostponeLight = false;
        }

        public override void Fire()
        {
            Task.Run(async () =>
            {
                DateTime sunriseMonitor = ConfigBuilder.Config.Sunrise;
                DateTime sunsetMonitor = ConfigBuilder.Config.Sunset;
                if (ConfigBuilder.Config.Location.Enabled)
                {
                    LocationHandler.GetSunTimesWithOffset(ConfigBuilder, out sunriseMonitor, out sunsetMonitor);
                }

                //the time between sunrise and sunset, aka "day"
                if (Extensions.NowIsBetweenTimes(sunriseMonitor.TimeOfDay, sunsetMonitor.TimeOfDay))
                {
                    if (SuntimeIsWithinSpan(sunsetMonitor) && !PostponeDark)
                    {
                        if (!PostponeDark)
                        {
                            Logger.Info($"starting GPU usage monitoring, theme switch pending within {Math.Abs(ConfigBuilder.Config.GPUMonitoring.MonitorTimeSpanMin)} minute(s)");
                            State.PostponeSwitch = true;
                            PostponeDark = true;
                        }
                    }
                    // if it's already light, check if the theme switch should be delayed
                    else if (PostponeLight && DateTime.Now >= sunriseMonitor)
                    {
                        var result = await CheckForPostpone(sunriseMonitor);
                        if (result != ThreshHigh)
                        {
                            PostponeLight = false;
                        }
                    }
                    else
                    {
                        if (PostponeDark || PostponeLight)
                        {
                            Logger.Info($"ending GPU usage monitoring");
                            PostponeDark = false;
                            PostponeLight = false;
                            State.PostponeSwitch = false;
                        }
                    }
                }
                // the time between sunset and sunrise, aka "night"
                else
                {
                    if (SuntimeIsWithinSpan(sunriseMonitor))
                    {
                        if (!PostponeLight)
                        {
                            Logger.Info($"starting GPU usage monitoring, theme switch pending within {Math.Abs(ConfigBuilder.Config.GPUMonitoring.MonitorTimeSpanMin)} minute(s)");
                            State.PostponeSwitch = true;
                            PostponeLight = true;
                        }
                    }
                    // if it's already dark, check if the theme switch should be delayed
                    else if (PostponeDark && DateTime.Now >= sunsetMonitor)
                    {
                        var result = await CheckForPostpone(sunsetMonitor);
                        if (result != ThreshHigh)
                        {
                            PostponeDark = false;
                        }
                    }
                    else
                    {
                        if (PostponeDark || PostponeLight)
                        {
                            Logger.Info($"ending GPU usage monitoring");
                            PostponeDark = false;
                            PostponeLight = false;
                            State.PostponeSwitch = false;
                        }
                    }
                }
            });
        }

        private async Task<string> CheckForPostpone(DateTime time)
        {
            var gpuUsage = await GetGPUUsage();
            if (gpuUsage <= ConfigBuilder.Config.GPUMonitoring.Threshold)
            {
                Counter++;
                if (Counter >= ConfigBuilder.Config.GPUMonitoring.Samples)
                {
                    Logger.Info($"ending GPU usage monitoring, re-enabling theme switch, threshold: {gpuUsage}% / {ConfigBuilder.Config.GPUMonitoring.Threshold}%");
                    State.PostponeSwitch = false;
                    return ThreshLow;
                }
                Logger.Debug($"lower threshold sample {Counter} ({gpuUsage}% / {ConfigBuilder.Config.GPUMonitoring.Threshold}%)");
            }
            else
            {
                Logger.Debug($"lower threshold sample reset ({gpuUsage}% / {ConfigBuilder.Config.GPUMonitoring.Threshold}%)");
                Counter = 0;
            }
            return ThreshHigh;
        }

        private async Task<int> GetGPUUsage()
        {
            var pcc = new PerformanceCounterCategory("GPU Engine");
            var counterNames = pcc.GetInstanceNames();
            List<PerformanceCounter> counters = new List<PerformanceCounter>();
            var counterAccu = 0f;
            foreach (string counterName in counterNames)
            {
                if (counterName.EndsWith("engtype_3D"))
                {
                    foreach (PerformanceCounter counter in pcc.GetCounters(counterName))
                    {
                        if (counter.CounterName == "Utilization Percentage")
                        {
                            counters.Add(counter);
                        }
                    }
                }
            }
            counters.ForEach(c =>
            {
                counterAccu += c.NextValue();
            });
            await Task.Delay(1000);
            counters.ForEach(c =>
            {
                counterAccu += c.NextValue();
            });
            counters.Clear();
            return (int)counterAccu;
        }

        /// <summary>
        /// checks whether a time is within a grace period (within x minutes before a DateTime)
        /// </summary>
        /// <param name="time">time to be checked</param>
        /// <returns>true if it's within the span; false otherwise</returns>
        private bool SuntimeIsWithinSpan(DateTime time)
        {
            return Extensions.NowIsBetweenTimes(
                time.AddMinutes(-Math.Abs(ConfigBuilder.Config.GPUMonitoring.MonitorTimeSpanMin)).TimeOfDay,
                time.TimeOfDay);
        }

        public override void Cleanup()
        {
            Logger.Debug($"cleanup performed for module {Name}");
            State.PostponeSwitch = false;
        }
    }
}

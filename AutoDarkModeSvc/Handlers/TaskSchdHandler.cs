using Microsoft.Win32.TaskScheduler;
using System;

namespace AutoDarkModeSvc.Handlers
{
    public static class TaskSchdHandler
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        static readonly string logon = "ADM Logon";
        static readonly string folder = "ADM_" + Environment.UserName;
        static readonly string author = "Armin Osaj";
        static readonly string program = "Windows Auto Dark Mode";
        static readonly string description = "Task of the program Windows Auto Dark Mode.";

        public static bool CreateLogonTask()
        {
            try
            {
                using TaskService taskService = new TaskService();
                taskService.RootFolder.CreateFolder(folder, null, false);

                TaskDefinition tdLogon = taskService.NewTask();

                tdLogon.RegistrationInfo.Description = "Switches theme at user logon, replaces old autostart entry. " + description;
                tdLogon.RegistrationInfo.Author = author;
                tdLogon.RegistrationInfo.Source = program;
                tdLogon.Settings.DisallowStartIfOnBatteries = false;
                tdLogon.Settings.ExecutionTimeLimit = TimeSpan.Zero;
                tdLogon.Settings.AllowHardTerminate = false;
                tdLogon.Settings.StartWhenAvailable = true;

                tdLogon.Triggers.Add(new LogonTrigger { UserId = Environment.UserDomainName + @"\" + Environment.UserName });
                tdLogon.Actions.Add(new ExecAction(Extensions.ExecutionPath));

                taskService.GetFolder(folder).RegisterTaskDefinition(logon, tdLogon);
                Logger.Info("created logon task");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "failed to create logon task, ");
            }
            return false;
        }

        public static bool RemoveLogonTask()
        {
            using TaskService taskService = new TaskService();
            TaskFolder taskFolder = taskService.GetFolder(folder);
            if (taskFolder == null)
            {
                Logger.Debug("logon task does not exist (no taskFolder)");
                return true;
            }
            try
            {
                taskFolder.DeleteTask(logon, false);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "failed removing logon task, ");
            }
            return false;
        }
    }
}

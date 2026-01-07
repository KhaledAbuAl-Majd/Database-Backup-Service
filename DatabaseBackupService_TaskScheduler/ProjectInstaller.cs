using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Configuration.Install;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;
using Microsoft.Win32.TaskScheduler;

namespace DatabaseBackupService_TaskScheduler
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : Installer
    {
        ServiceProcessInstaller processInstaller;
        ServiceInstaller serviceInstaller;

        public ProjectInstaller()
        {
            InitializeComponent();

            processInstaller = new ServiceProcessInstaller()
            {
                //Account = ServiceAccount.LocalService
                Account = ServiceAccount.LocalSystem
            };

            serviceInstaller = new ServiceInstaller()
            {
                ServiceName = clsGlobal.ServiceName,
                DisplayName = "Database Backup Service TS",
                Description = "Backup Database for Interval (Task Scheduler)",
                StartType = ServiceStartMode.Manual,//task scheduler will raise it
                ServicesDependedOn = new string[] { "RpcSs", "EventLog", "MSSQLSERVER" }
            };

            Installers.Add(processInstaller);
            Installers.Add(serviceInstaller);
        }

        protected override void OnAfterInstall(IDictionary savedState)
        {
            string serviceName = clsGlobal.ServiceName;

            base.OnAfterInstall(savedState);

            short backupIntervalDays = 1;
            if (short.TryParse(ConfigurationManager.AppSettings["BackupIntervalDays"], out short result))
            {
                backupIntervalDays = result;
            }
            else
            {
                string exePath = Context.Parameters["assemblypath"];
                Configuration config = ConfigurationManager.OpenExeConfiguration(exePath);
                string value = config.AppSettings.Settings["BackupIntervalDays"]?.Value;

                if (short.TryParse(value, out short result2))
                {
                    backupIntervalDays = result2;
                }
            }

            string command = $"failure {serviceName} reset= 86400 actions= restart/6000/restart/10000/\"\"/0";

            //execute without try..catch (it's mandotry to executes successfully)
            using (var process = new System.Diagnostics.Process())
            {
                process.StartInfo.FileName = "sc";
                process.StartInfo.Arguments = command;
                process.StartInfo.Verb = "runas";
                process.Start();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    throw new Exception("Configuration failed with Exit Code: " + process.ExitCode);
                }
            }

            using (TaskService ts = new TaskService())
            {
                TaskDefinition td = ts.NewTask();
                td.RegistrationInfo.Author = "Khaled Abu Al-Majd";
                td.RegistrationInfo.Description = "Automated Database Backup";

                td.Principal.RunLevel = TaskRunLevel.Highest;

                td.Triggers.Add(new DailyTrigger()
                {
                    StartBoundary = DateTime.Now,
                    DaysInterval = backupIntervalDays
                });

                td.Settings.AllowDemandStart = true;
                td.Settings.StartWhenAvailable = true;

                td.Actions.Add("sc", arguments: $"start {serviceName}");

                ts.RootFolder.RegisterTaskDefinition(serviceName, td);
            }
        }

        protected override void OnAfterUninstall(IDictionary savedState)
        {
            base.OnAfterUninstall(savedState);

            try
            {
                using (TaskService ts = new TaskService())
                {
                    ts.RootFolder.DeleteTask(serviceInstaller.ServiceName, true);
                }
            }
            catch 
            {

            }
        }
    }
}

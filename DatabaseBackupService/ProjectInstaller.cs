using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace DatabaseBackupService
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
                ServiceName = "DatabaseBackupService",
                DisplayName = "Database Backup Service",
                Description = "Backup Database every num of minutes",
                StartType = ServiceStartMode.Automatic,
                ServicesDependedOn = new string[] { "RpcSs", "EventLog", "MSSQLSERVER" }
            };

            Installers.Add(processInstaller);
            Installers.Add(serviceInstaller);
        }

        protected override void OnAfterInstall(IDictionary savedState)
        {
            base.OnAfterInstall(savedState);
            string command = $"failure {serviceInstaller.ServiceName} reset= 86400 actions= restart/6000/restart/10000/\"\"/0";

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
        }
    }
}

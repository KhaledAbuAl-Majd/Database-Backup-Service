using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.TaskScheduler;

namespace DatabaseBackupService_TaskScheduler
{
    public partial class DatabaseBackupService_TS : ServiceBase
    {
        string connectionString;
        string logFilePath;
        string backupFolder;
        public DatabaseBackupService_TS()
        {
            InitializeComponent();

            connectionString = ConfigurationManager.AppSettings["ConnectionString"];
            backupFolder = ConfigurationManager.AppSettings["BackupFolder"];
            string logFolder = ConfigurationManager.AppSettings["LogFolder"];

            try
            {
                if (string.IsNullOrWhiteSpace(backupFolder))
                {
                    throw new ArgumentException("backup folder is empty");
                }

                if (string.IsNullOrWhiteSpace(logFolder))
                {
                    throw new ArgumentException("log folder is empty");
                }

                if (!Directory.Exists(backupFolder))
                {
                    Directory.CreateDirectory(backupFolder);
                }

                if (!Directory.Exists(logFolder))
                {
                    Directory.CreateDirectory(logFolder);
                }

            }
            catch (Exception ex)
            {
                Log(ex.Message);
                Environment.Exit(1);
            }

            logFilePath = Path.Combine(logFolder, "ServiceLog.txt");
        }

        void PerformBackup()
        {
            try
            {
                string backupFileName = "";

                using (var connection = new SqlConnection(connectionString))
                {
                    string folerPath = Path.Combine(backupFolder, connection.Database);
                    Directory.CreateDirectory(folerPath);

                    backupFileName = Path.Combine(folerPath, $"Backup_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.bak");

                    string query = $"Backup DATABASE [{connection.Database}] TO DISK = '{backupFileName}' WITH INIT";

                    using (var command = new SqlCommand(query, connection))
                    {
                        //await connection.OpenAsync();
                        //await command.ExecuteNonQueryAsync();
                         connection.Open();
                         command.ExecuteNonQuery();
                    }
                }

                Log($"Database backup successful: {backupFileName}");
            }
            catch (Exception ex)
            {
                Log($"Database backup failed, Exception: {ex.Message}");
            }
            finally
            {
                this.Stop();
            }
        }

        void Log(string message)
        {
            string logMessage = $"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}] {message}\n";
            try
            {
                File.AppendAllText(logFilePath, logMessage);
            }
            catch
            {

            }

            if (Environment.UserInteractive)
            {
                Console.WriteLine(logMessage);
            }

        }

        protected override void OnStart(string[] args)
        {
            Log("Service Started");

            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal;

            Thread worker = new Thread(PerformBackup);
            worker.Start();
        }

        protected override void OnStop()
        {
            Log("Service Stopped");
            //_ = System.Threading.Tasks.Task.Run(() => UpdateTaskSchedulerInterval());
            UpdateTaskSchedulerInterval();
        }

        private void UpdateTaskSchedulerInterval()
        {
            using(TaskService ts = new TaskService())
            {
                var existingTask = ts.GetTask(clsGlobal.ServiceName);

                if (existingTask != null)
                {
                    short newDaysInterval = 0;
                    if (short.TryParse(ConfigurationManager.AppSettings["BackupIntervalDays"], out short result))
                    {
                        newDaysInterval = result;
                    }
                    else
                    {
                        Log($"Invalid [BackupIntervalDays] - it can't be update");
                        return;
                    }

                    TaskDefinition td = existingTask.Definition;
                    if (td.Triggers.Count > 0 && td.Triggers[0] is DailyTrigger dailyTrigger)
                    {
                        dailyTrigger.DaysInterval = newDaysInterval;
                    }

                    ts.RootFolder.RegisterTaskDefinition(
                    existingTask.Name,
                    td,
                    TaskCreation.Update,
                    null, null, TaskLogonType.S4U);

                    Log($"Task {existingTask.Name} updated successfully to {newDaysInterval} days.");
                }
                else
                {
                    Log("Access Denied or Task not found. Please ensure the app is running with Administrative privileges.");
                }
            }
        }

        public void StartInConsole()
        {
            OnStart(null);
            Console.WriteLine("press enter to contiure...");
            Console.ReadLine();
            OnStop();
        }
    }
}

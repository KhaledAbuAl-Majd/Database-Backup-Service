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

namespace DatabaseBackupService
{
    public partial class DatabaseBackupService : ServiceBase
    {
        Timer backupTimer;
        string connectionString;
        string logFilePath;
        string backupFolder;
        int backupIntervalMinutes;
        public DatabaseBackupService()
        {
            InitializeComponent();

            //CanPauseAndContinue = true;
            //CanShutdown = true;

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

                if (int.TryParse(ConfigurationManager.AppSettings["BackupIntervalMinutes"], out int result))
                {
                    backupIntervalMinutes = result;
                }
                else
                {
                    throw new ArgumentException("InValid [BackupIntervalMinutes]");
                }
            }
            catch (Exception ex)
            {
                Log(ex.Message);
                Environment.Exit(1);
            }

            logFilePath = Path.Combine(logFolder, "ServiceLog.txt");
        }

        async Task PerformBackup()
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
                        await connection.OpenAsync();
                        await command.ExecuteNonQueryAsync();
                    }
                }

                Log($"Database backup successful: {backupFileName}");
            }
            catch (Exception ex)
            {
                Log($"Database backup failed, Exception: {ex.Message}");
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

        private async void TimerCallBack(object state)
        {
            backupTimer.Change(Timeout.Infinite, Timeout.Infinite);
            await PerformBackup();
            backupTimer.Change(TimeSpan.FromMinutes(backupIntervalMinutes), TimeSpan.FromMinutes(backupIntervalMinutes));
        }
        protected override void OnStart(string[] args)
        {
            Log("Service Started");

            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal;

            backupTimer = new Timer(callback: TimerCallBack, state: null, dueTime: TimeSpan.Zero, period: TimeSpan.FromMinutes(backupIntervalMinutes));

            Log($"Backup schedule initiated: every {backupIntervalMinutes} minute(s).");
        }

        protected override void OnStop()
        {
            if (backupTimer != null)
            {
                backupTimer.Dispose();
            }
            Log("Service Stopped");
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

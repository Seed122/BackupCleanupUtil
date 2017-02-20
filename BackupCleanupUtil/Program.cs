using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NLog;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;

// start it with cmd argument like "C:\backups"

namespace BackupCleanupUtil
{
    class Program
    {
        private static Logger _logger;
        public static int _deleteCount = 0;

        static void Main(string[] args)
        {
            ConfigNLog();
            if (args.Length != 1)
            {
                _logger.Error("Pass directory path via args");
                return;
            }

            var dirPath = args[0];
            if (!Directory.Exists(dirPath))
            {
                _logger.Error("Directory does not exist");
                return;
            }

            if (Settings.DebugMode)
            {
                Populate(dirPath);
            }

            var allfiles = Directory.GetFiles(dirPath);

            var objects = new Dictionary<string, ICollection<BackupFile>>();
            foreach (var filePath in allfiles)
            {
                var match = Regex.Match(filePath, @"^.+\\(.+)_?(\d{8})\.\w+$");
                if (!match.Success) continue;
                string obj = match.Groups[1].Value;
                string dateStr = match.Groups[2].Value;
                var date = DateTime.ParseExact(dateStr, "yyyyMMdd", CultureInfo.InvariantCulture);
                if (!objects.ContainsKey(obj))
                {
                    objects.Add(obj, new List<BackupFile>());
                }
                objects[obj].Add(new BackupFile(filePath, date));
            }

            var today = DateTime.Now.Date;
            const int WEEK_LENGTH = 7;
            const int MONTH_LENGTH = 30;


            foreach (var obj in objects)
            {
                var remainingBackups = new List<BackupFile>(obj.Value);
                var recentBaks = obj.Value.Where(x => x.Date > today.AddDays(-Settings.RotationDays));
                remainingBackups.RemoveAll(x => recentBaks.Contains(x));
                var weekBakSets = new Dictionary<int, List<BackupFile>>();
                for (int i = 0; i < Settings.RotationWeeks; i++)
                {
                    DateTime weekEnd = today.AddDays(-(WEEK_LENGTH * i + Settings.RotationDays));
                    DateTime weekStart = weekEnd.AddDays(-WEEK_LENGTH);
                    if (!weekBakSets.ContainsKey(i))
                    {
                        weekBakSets.Add(i, new List<BackupFile>());
                    }
                    weekBakSets[i].AddRange(obj.Value.Where(x => x.Date > weekStart && x.Date <= weekEnd));
                    remainingBackups.RemoveAll(x => weekBakSets[i].Contains(x));
                }
                var monthBakSets = new Dictionary<int, List<BackupFile>>();
                for (int i = 0; i < Settings.RotationMonths; i++)
                {
                    DateTime monthEnd =
                        today.AddDays(-(WEEK_LENGTH * Settings.RotationWeeks + Settings.RotationDays)).AddDays(-(i * MONTH_LENGTH));
                    DateTime monthStart =
                        monthEnd.AddDays(-MONTH_LENGTH);
                    if (!monthBakSets.ContainsKey(i))
                    {
                        monthBakSets.Add(i, new List<BackupFile>());
                    }
                    monthBakSets[i].AddRange(obj.Value.Where(x => x.Date > monthStart && x.Date <= monthEnd));
                    remainingBackups.RemoveAll(x => monthBakSets[i].Contains(x));
                }

                // do nothing with recent backups

                // delete from weeks except last
                foreach (var bakSet in weekBakSets)
                {
                    CleanupPeriod(bakSet.Value);
                }

                // delete from months except last
                foreach (var bakSet in monthBakSets)
                {
                    CleanupPeriod(bakSet.Value);
                }

                // delete all remaining backups
                foreach (var backupFile in remainingBackups)
                {
                    DeleteBackup(backupFile.FilePath);
                }
            }
            Write($"Deleted {_deleteCount} backups");
        }

        private static void ConfigNLog()
        {
            const string TARGET_NAME = "Default";
            var config = new LoggingConfiguration();
            config.AddTarget(TARGET_NAME, new FileTarget()
            {
                Layout = Layout.FromString(@"${longdate} - ${level:uppercase=true}: ${message}${onexception:${newline}EXCEPTION\: ${exception:format=ToString}}"),
                FileName = Layout.FromString(@"${basedir}/logs/${shortdate}.log"),
                KeepFileOpen = false,
                Encoding = Encoding.UTF8,
                Name = TARGET_NAME,
                CreateDirs = true,
                
            });
            config.AddRuleForAllLevels(TARGET_NAME);
            LogManager.Configuration = config;
            
            _logger = LogManager.GetLogger("Default");
        }

        private static void Write(string msg)
        {
            Console.WriteLine(msg);
            _logger.Trace(msg);
        }

        private static void CleanupPeriod(ICollection<BackupFile> periodBackups)
        {
            if (!periodBackups.Any()) return;
            var baksToDelete = periodBackups.Where(x => !x.Equals(periodBackups.OrderByDescending(y => y.Date).Last())).ToList();
            foreach (var backupFile in baksToDelete)
            {
                DeleteBackup(backupFile.FilePath);
                periodBackups.Remove(backupFile);
            }
        }

        private static void DeleteBackup(string filePath)
        {
            Write($"Deleting {filePath}");
            File.Delete(filePath);
            _deleteCount++;
        }

        private static void Populate(string dir)
        {
            var today = DateTime.Now.Date;
            const int BACKUP_DAYS = 365;
            var objectNames = new[] { "object1" };
            for (int i = 0; i < BACKUP_DAYS; i++)
            {
                foreach (var objectName in objectNames)
                {
                    var path = Path.Combine(dir, $"{objectName}_{today.AddDays(-i).ToString("yyyyMMdd")}.bak");
                    using (var f = File.Create(path))
                    { }
                }
            }
            var fileCount = BACKUP_DAYS*objectNames.Length;
            Write($"Populated with {fileCount} files");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackupCleanupUtil
{
    public static class Settings
    {
        public static bool DebugMode => bool.Parse(ConfigurationManager.AppSettings[nameof(DebugMode)]);
        public static int RotationDays => int.Parse(ConfigurationManager.AppSettings[nameof(RotationDays)]);
        public static int RotationWeeks => int.Parse(ConfigurationManager.AppSettings[nameof(RotationWeeks)]);
        public static int RotationMonths => int.Parse(ConfigurationManager.AppSettings[nameof(RotationMonths)]);
    }
}

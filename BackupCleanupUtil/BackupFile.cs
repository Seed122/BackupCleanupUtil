using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackupCleanupUtil
{
    public class BackupFile
    {
        public BackupFile(string filePath, DateTime date)
        {
            FilePath = filePath;
            Date = date;
        }

        public string FilePath { get; set; }
        public DateTime Date { get; set; }

        public override string ToString()
        {
            return FilePath;
        }
    }
}

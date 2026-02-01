using System;
using System.IO;

namespace LeaveManager.Data.Storage
{
    public static class DbBackup
    {
        public static string CreateBackup(string dbPath)
        {
            var folder = DbPaths.GetDbFolderPath();

            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmm");
            var backupFileName = $"data_{timestamp}.db";
            var backupPath = Path.Combine(folder, backupFileName);

            File.Copy(dbPath, backupPath, overwrite: false);
            return backupPath;
        }

        public static void RestoreBackup(string backupPath, string dbPath)
        {
            // Restore: overwrite the DB with the backup
            File.Copy(backupPath, dbPath, overwrite: true);
        }
    }
}

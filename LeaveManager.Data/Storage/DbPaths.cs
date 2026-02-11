using System;
using System.IO;

namespace LeaveManager.Data.Storage
{
    public static class DbPaths
    {
        // returns %LocalAppData%\LeaveManager
        public static string GetDbFolderPath()
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var folder = Path.Combine(localAppData, "LeaveManager");

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            return folder;
        }

        // returns full database file path
        public static string GetDbFilePath()
        {
            return Path.Combine(GetDbFolderPath(), "data.db");
        }

        // canonical property used by migration runner and app startup
        public static string DatabasePath => GetDbFilePath();
    }
}
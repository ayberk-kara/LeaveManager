using System;
using System.IO;

namespace LeaveManager.Data.Storage
{
    public static class DbPaths
    {
        // %LocalAppData%\LeaveManager
        public static string GetDbFolderPath()
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, "LeaveManager");
        }

        // %LocalAppData%\LeaveManager\data.db
        public static string GetDbFilePath()
        {
            return Path.Combine(GetDbFolderPath(), "data.db");
        }
    }
}

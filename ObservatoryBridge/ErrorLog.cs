using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Observatory.Bridge
{
    internal static class ErrorLog
    {
        public static void LogException(this Exception ex)
        {
            var date = DateTime.Today.ToString("yyyy-MM-dd");
            var time = DateTime.Now.ToString("HH:mm:ss.ffff");
            var filename = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Elite Observatory", "Bridge", $"ErrorLog_{date}.txt");
            if (!Directory.Exists(Path.GetDirectoryName(filename)))
                Directory.CreateDirectory(Path.GetDirectoryName(filename));

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"[{date} {time}] ---------------------------------------");
            sb.AppendLine(ex.ToString());
            sb.AppendLine("---------------------------------------");

            File.AppendAllText(filename, sb.ToString());
        }

        public static void LogInfo(string msg)
        {
            var date = DateTime.Today.ToString("yyyy-MM-dd");
            var time = DateTime.Now.ToString("HH:mm:ss.ffff");
            var filename = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Elite Observatory", "Bridge", $"InfoLog_{date}.txt");
            if (!Directory.Exists(Path.GetDirectoryName(filename)))
                Directory.CreateDirectory(Path.GetDirectoryName(filename));

            File.AppendAllText(filename, $"[{date} {time}] {msg}\r\n");
        }
    }
}

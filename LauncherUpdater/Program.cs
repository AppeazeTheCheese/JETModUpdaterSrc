

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Reflection;

namespace LauncherUpdater
{
    class Program
    {
        private const string downloadRoot = "https://github.com/AppeazeTheCheese/JETModUpdater/releases/download";
        private const string versionFile = "https://raw.githubusercontent.com/AppeazeTheCheese/JETModUpdater/main/version.txt";
        private static readonly WebClient _client = new WebClient { CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore)};

        static void Main(string[] args)
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

            if (args.Length < 1)
                return;

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            Console.Title = "Launcher Updater";
            var pid = int.Parse(args[1]);
            Process proc = null;
            try
            {
                proc = Process.GetProcessById(pid);
                Console.WriteLine("Waiting for Launcher.exe to close...");
            }
            catch { }

            proc?.WaitForExit();

            var finalDownloadPath = args[0];

            var versionString = string.Join("", _client.DownloadString(versionFile).Where(x => char.IsDigit(x) || x == '.'));

            var fullDownloadUri = new Uri(downloadRoot + $"/{versionString}/Launcher.exe", UriKind.Absolute);
            var tmpDownloadPath = Path.GetTempFileName();

            Console.WriteLine("Downloading update to temporary file...");
            _client.DownloadFile(fullDownloadUri, tmpDownloadPath);

            Console.WriteLine("Replacing existing file with the updated file...");
            if(File.Exists(finalDownloadPath))
                File.Delete(finalDownloadPath);
            File.Move(tmpDownloadPath, finalDownloadPath);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Successfully updated! Starting launcher...");
            proc = new Process();
            var startInfo = new ProcessStartInfo(finalDownloadPath) {WorkingDirectory = Path.GetDirectoryName(finalDownloadPath), Verb = "", CreateNoWindow = false};



            proc.StartInfo = startInfo;
            proc.Start();

            Process.Start(new ProcessStartInfo
            {
                Arguments = "/C choice /C Y /N /D Y /T 3 & Del \"" + Assembly.GetExecutingAssembly().Location + "\"",
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                FileName = "cmd.exe"
            });
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Failed to update the launcher due to the following error: {((Exception)e.ExceptionObject).Message}");
            Console.WriteLine();
            Console.Write("Please manually download the newest release from https://github.com/AppeazeTheCheese/JETModUpdater/releases");
            Console.ReadKey();
            Environment.Exit(1);
        }
    }
}

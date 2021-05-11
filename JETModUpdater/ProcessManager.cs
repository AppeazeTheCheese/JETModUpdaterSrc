using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace JETModUpdater
{
    public static class ProcessManager
    {
        private static Process proc = null;
#if HcBuild
        public static bool restartProcess { private get; set; } = false;
        public static bool shutdownProcess { private get; set; } = false;
#endif
        public static void StartServer()
        {
            proc = new Process();
            var info = new ProcessStartInfo(Path.Combine(Program.DirPath, "Server.exe"))
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                StandardErrorEncoding = Encoding.UTF8,
                StandardOutputEncoding = Encoding.UTF8,
                WorkingDirectory = Program.DirPath
            };


            proc.StartInfo = info;
            proc.OutputDataReceived += Proc_OutputDataReceived;
            proc.ErrorDataReceived += Proc_OutputDataReceived;

            FindAndKillInstances();

            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            var job = new Job();
            job.AddProcess(proc.Handle);

            try
            {
                proc.WaitForExit();
            }
            catch { }

#if HcBuild
            if (restartProcess)
            {
                Console.WriteLine("Server has exited. Restarting...");
                try
                {
                    proc.Dispose();
                } catch { }
                Program.ClearCache();
                new Thread(StartServer).Start();
            }
            else if (shutdownProcess)
            {
                Console.WriteLine("Server has exited. Press any key to restart it...");
                Console.ReadKey();
                try
                {
                    proc.Dispose();
                } catch { }
                Program.ClearCache();
                new Thread(StartServer).Start();
            }
            else
            {
                Console.WriteLine("Server has exited. Press any key to continue...");
                Console.ReadKey();
                Environment.Exit(0);
            }

            restartProcess = false;
            shutdownProcess = false;
#else
            Console.WriteLine("Server has exited. Press any key to continue...");
            Console.ReadKey();
            Environment.Exit(0);
#endif
        }

        private static void FindAndKillInstances()
        {
            var procs = Process.GetProcessesByName("Server");

            var serverProcs = procs.Where(x => string.Equals(x.MainModule.FileName, Path.Combine(Program.DirPath, "Server.exe"), StringComparison.CurrentCultureIgnoreCase));
            foreach (var p in serverProcs)
                try { p.Kill(); } catch { }
        }

        public static void WriteInput(string text)
        {
            proc?.StandardInput.WriteLineAsync(text);
        }
        private static void Proc_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            AnsiColorManager.WriteColorizedString(e.Data);
        }
    }

}
//#define HcBuild
//#undef HcBuild

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace JETModUpdater
{
    class Program
    {
#if DEBUG
        public const string DirPath = @"C:\Emu Tarkov\1.0.2\Modded\Server";
#else
        public static readonly string DirPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
#endif
        private static readonly Version currentVersion = new Version(1, 0, 5);
        private const string VersionFile = "https://raw.githubusercontent.com/AppeazeTheCheese/JETModUpdater/main/version.txt";


        private static readonly WebClient Client = new WebClient { CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore) };
        public static readonly string ModsDir = Path.Combine(DirPath, "user/mods");
        private static bool _modDebug = false;
#if HcBuild
        private static bool _updateLauncher = false;
#else
        private static bool _updateLauncher = true;
#endif
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Console.Title = "Auto Update Launcher";
            Console.OutputEncoding = Encoding.UTF8;

            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            new Thread(HandleCommands).Start();


            #region Argument Parsing
            foreach (var arg in args.Select(x => x.ToLower()))
            {
                switch (arg)
                {
                    case "-debug":
                    case "debug":
                    case "dbg":
                        _modDebug = true;
                        break;
                    case "-noupdate":
                    case "noupdate":
                    case "nu":
                        _updateLauncher = false;
                        break;
                }
            }
            #endregion

            #region Update Launcher
            if (!_modDebug && _updateLauncher)
            {
                // Check for launcher update
                Logger.WriteConsole(Logger.LogType.Info, "Checking for launcher updates...");
                var versionStr = string.Empty;
                try
                {
                    versionStr = Client.DownloadString(VersionFile);
                }
                catch { }

                if (!string.IsNullOrWhiteSpace(versionStr))
                {
                    var remoteVersion = Version.Parse(versionStr);
                    if (remoteVersion > currentVersion)
                    {
                        Logger.WriteConsole(Logger.LogType.Info, "Launcher update found! Launching update program...");
                        var embeddedName = Assembly.GetExecutingAssembly().GetManifestResourceNames().First(x => x.EndsWith("LauncherUpdater.exe"));
                        var tmpFileName = Path.GetTempFileName();
                        var tmpExeName = Path.ChangeExtension(tmpFileName, ".exe");
                        File.Move(tmpFileName, tmpExeName);

                        var fileStream = File.OpenWrite(tmpExeName);
                        Assembly.GetExecutingAssembly().GetManifestResourceStream(embeddedName).CopyTo(fileStream);
                        fileStream.Close();

                        new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = tmpExeName,
                                Arguments =
                                    $"\"{Assembly.GetExecutingAssembly().Location}\" {Process.GetCurrentProcess().Id}",
                                Verb = ""
                            }
                        }.Start();

                        Environment.Exit(0);
                    }
                    else
                    {
                        Logger.WriteConsole(Logger.LogType.Info, "Your launcher is up to date");
                    }
                }
            }

            #endregion

            #region Update Mods

            var modFolders = Directory.GetDirectories(ModsDir);

            foreach (var folder in modFolders)
            {
                #region Parse Local mod.config.json
                var folderPath = Path.Combine(ModsDir, folder);
                var configFilePath = Path.Combine(folderPath, "mod.config.json");

                if (!File.Exists(configFilePath))
                    continue;

                UpdateInfo info;

                try
                {
                    var jsonText = File.ReadAllText(configFilePath);
                    info = JsonConvert.DeserializeObject<UpdateInfo>(jsonText);
                }
                catch
                {
                    info = new UpdateInfo();
                }
                #endregion

                #region Check Integrity of Local mod.config.json
                if (info.CheckUpdateUrl == null)
                    continue;
                if (!info.AutoUpdate)
                    continue;
                if (!Uri.IsWellFormedUriString(info.CheckUpdateUrl, UriKind.Absolute))
                {
                    Logger.WriteConsole(Logger.LogType.Warning, $"[{info.Name}] The \"updateCheckUrl\" is not set. Skipping.");
                    continue;
                }

                if (info.Version == null)
                {
                    Logger.WriteConsole(Logger.LogType.Warning, $"[{info.Name}] The local version number is in an incorrect format. Skipping.");
                    continue;
                }
                #endregion

                #region Check Remote Version
                var url = new Uri(info.CheckUpdateUrl);

                string dataString;
                try
                {
                    dataString = Client.DownloadString(url);
                }
                catch (Exception e)
                {
                    Logger.WriteConsole(Logger.LogType.Error, $"[{info.Name}] Error while attempting to retrieve file {url.OriginalString}: " + e.Message);
                    continue;
                }

                UpdateInfo remoteInfo;

                try
                {
                    remoteInfo = JsonConvert.DeserializeObject<UpdateInfo>(dataString);
                }
                catch (Exception e)
                {
                    Logger.WriteConsole(Logger.LogType.Error, $"[{info.Name}] Failed to read the remote config file: {e.Message}");
                    continue;
                }

                if (remoteInfo.Version == null)
                {
                    Logger.WriteConsole(Logger.LogType.Warning, $"[{info.Name}] The remote version number is in an incorrect format. Skipping.");
                    continue;
                }

                var update = false;

                if (remoteInfo.Version != info.Version && remoteInfo.ForceDowngrade)
                    update = true;
                else if (remoteInfo.Version > info.Version)
                    update = true;

                if (!update)
                {
                    Logger.WriteConsole(Logger.LogType.Success, $"{info.Name} is up to date.");
                    continue;
                }
                #endregion

                #region Check Integrity of Remote JSON File
                Logger.WriteConsole(Logger.LogType.Info, $"Updating {info.Name} to version {remoteInfo.Version}...");
                if (string.IsNullOrWhiteSpace(remoteInfo.DownloadUpdateUrl))
                {
                    Logger.WriteConsole(Logger.LogType.Warning, $"[{info.Name}] The remote config's \"downloadUpdateUrl\" is not set. Skipping");
                    continue;
                }

                if (!Uri.IsWellFormedUriString(remoteInfo.DownloadUpdateUrl, UriKind.Absolute))
                {
                    Logger.WriteConsole(Logger.LogType.Warning, $"[{info.Name}] The remote config's \"downloadUpdateUrl\" is in an incorrect format. Skipping.");
                    continue;
                }
                #endregion

                #region Download ZIP File
                var downloadUrl = new Uri(remoteInfo.DownloadUpdateUrl);
                Logger.WriteConsole(Logger.LogType.Info, $"[{info.Name}] Downloading update from {downloadUrl.OriginalString}...");

                byte[] data;
                try
                {
                    data = Client.DownloadData(downloadUrl);
                }
                catch (Exception e)
                {
                    Logger.WriteConsole(Logger.LogType.Error, $"[{info.Name}] Error occurred while trying to download the update file from {downloadUrl}: " + e.Message);
                    continue;
                }

                if (string.IsNullOrEmpty(Client.ResponseHeaders["Content-Disposition"]))
                {
                    Logger.WriteConsole(Logger.LogType.Error, $"[{info.Name}] Content-Disposition header is missing. The download URL may be incorrect. Skipping.");
                    continue;
                }

                var fileName = Client.ResponseHeaders["Content-Disposition"].Substring(Client.ResponseHeaders["Content-Disposition"].IndexOf("filename=", StringComparison.Ordinal) + 9).Replace("\"", "");
                var filePath = Path.Combine(ModsDir, fileName);
                Logger.WriteConsole(Logger.LogType.Write, $"[{info.Name}] Writing to {filePath}");

                File.WriteAllBytes(filePath, data);
                #endregion

                #region Extract ZIP File
                Logger.WriteConsole(Logger.LogType.Info, $"[{info.Name}] Extracting downloaded file...");

                #region Open Archive
                ZipArchive zipFile;
                try
                {
                    zipFile = ZipFile.Open(filePath, ZipArchiveMode.Read);
                }
                catch (Exception e)
                {
                    try { File.Delete(filePath); } catch { }
                    Logger.WriteConsole(Logger.LogType.Error, $"[{info.Name}] An error occurred while trying to unpack the downloaded zip file: " + e.Message);
                    continue;
                }
                var configFile = zipFile.Entries.Where(x => x.Name.ToLower().EndsWith("mod.config.json"));
                if (!configFile.Any())
                {
                    Logger.WriteConsole(Logger.LogType.Error, $"[{info.Name}] Could not find the mod's config file in the downloaded archive. Cancelling update.");
                    try { File.Delete(filePath); } catch { }
                    continue;
                }
                #endregion

                #region Find Actual Mod Path in Archive
                var firstConfigFile = configFile.First();
                var containingFolder = string.Empty;
                var modFolderName = remoteInfo.Author + "-" + remoteInfo.Name + "-" + remoteInfo.Version;

                if (firstConfigFile.FullName.Contains("/"))
                {
                    var splitPath = firstConfigFile.FullName.Split('/');
                    containingFolder = string.Join("/", splitPath.Take(splitPath.Length - 1));
                }
                #endregion


                var fullModDir = Path.Combine(ModsDir, modFolderName);
                if (!Directory.Exists(fullModDir))
                    Directory.CreateDirectory(fullModDir);

                #region Unpack Archive
                if (string.IsNullOrWhiteSpace(containingFolder))
                {

                    foreach (var entry in zipFile.Entries)
                    {
                        var exclude =
                            remoteInfo.ExcludeFromUpdate.Count(x =>
                            {
                                if (entry.FullName.EndsWith("/") || entry.FullName.EndsWith("\\") ||
                                    !Path.HasExtension(x))
                                    return entry.FullName.ToLower().StartsWith(x.ToLower());
                                return entry.FullName.ToLower().Contains(x.ToLower());
                            }) > 0;
                        var entryPath = Path.Combine(fullModDir, entry.FullName);
                        if (entry.FullName.EndsWith("/"))
                            continue;
                        if (exclude)
                            continue;
                        Logger.WriteConsole(Logger.LogType.Write, $"[{info.Name}] Writing to {entry.FullName}");
                        var entryStream = entry.Open();
                        if (!Directory.Exists(Path.GetDirectoryName(entryPath)))
                            Directory.CreateDirectory(Path.GetDirectoryName(entryPath));
                        if (File.Exists(entryPath))
                            File.Delete(entryPath);
                        var fileStream = File.OpenWrite(entryPath);
                        entryStream.CopyTo(fileStream);
                        entryStream.Close();
                        fileStream.Close();
                        fileStream.Dispose();
                    }
                }
                else
                {
                    foreach (var entry in zipFile.Entries.Where(x => x.FullName.ToLower().StartsWith(containingFolder.ToLower())))
                    {
                        var regex = new Regex(Regex.Escape(containingFolder + "/"));
                        var actualRelativePath = regex.Replace(entry.FullName, "", 1);
                        if (entry.FullName.EndsWith("/"))
                            continue;
                        var exclude =
                            remoteInfo.ExcludeFromUpdate.Count(x =>
                            {
                                if (actualRelativePath.EndsWith("/") || actualRelativePath.EndsWith("\\") ||
                                    !Path.HasExtension(x))
                                    return actualRelativePath.ToLower().StartsWith(x.ToLower());
                                return actualRelativePath.ToLower().Contains(x.ToLower());
                            }) > 0;
                        var entryPath = Path.Combine(fullModDir, actualRelativePath);

                        if (exclude)
                            continue;
                        Logger.WriteConsole(Logger.LogType.Write, $"[{info.Name}] Writing to {actualRelativePath}");
                        var entryStream = entry.Open();
                        if (!Directory.Exists(Path.GetDirectoryName(entryPath)))
                            Directory.CreateDirectory(Path.GetDirectoryName(entryPath));
                        if (File.Exists(entryPath))
                            File.Delete(entryPath);
                        var fileStream = File.OpenWrite(entryPath);
                        entryStream.CopyTo(fileStream);
                        entryStream.Close();
                        fileStream.Close();
                        fileStream.Dispose();
                    }
                }
                #endregion

                #region Clean Up
                zipFile.Dispose();
                File.Delete(filePath);
                #endregion

                #region Move Files if Necessary
                if (fullModDir != folderPath)
                {
                    foreach (var item in remoteInfo.ExcludeFromUpdate)
                    {
                        var path = Path.Combine(folderPath, item);
                        if (!File.Exists(path) && !Directory.Exists(path)) continue;
                        var isFile = Path.HasExtension(path);
                        if (isFile)
                        {
                            Logger.WriteConsole(Logger.LogType.Move, $"[{info.Name}] Moving file {item} to new installation");
                            var newFile = Path.Combine(fullModDir, item);
                            if (File.Exists(newFile))
                                File.Delete(newFile);
                            var dir = Path.GetDirectoryName(newFile);
                            if (!Directory.Exists(newFile))
                                Directory.CreateDirectory(dir);
                            try
                            {
                                File.Move(path, newFile);
                            }
                            catch (Exception e)
                            {
                                Logger.WriteConsole(Logger.LogType.Error, $"[{info.Name}] Failed to move file {path} to {newFile}: {e.Message}");
                            }
                        }
                        else
                        {
                            Logger.WriteConsole(Logger.LogType.Move, $"[{info.Name}] Moving folder {item} to new installation");
                            var newFolder = Path.Combine(fullModDir, item);
                            if (Directory.Exists(newFolder))
                                Directory.Delete(newFolder, true);
                            try
                            {
                                Directory.Move(path, newFolder);
                            }
                            catch (Exception e)
                            {
                                Logger.WriteConsole(Logger.LogType.Error, $"[{info.Name}] Failed to move folder {path} to {newFolder}: {e.Message}");
                            }
                        }
                    }

                    try
                    {
                        Directory.Delete(folderPath, true);
                    }
                    catch (Exception e)
                    {
                        Logger.WriteConsole(Logger.LogType.Warning, $"[{info.Name}] Failed to delete previous version's install folder: " + e.Message);
                    }
                }
                #endregion

                Logger.WriteConsole(Logger.LogType.Success, $"[{info.Name}] successfully updated!");
            }
            #endregion

            ClearCache();

            #region Start JET Server
            Logger.WriteConsole(Logger.LogType.Info, "Starting JET server...");
            if (_modDebug)
            {
                Console.WriteLine("Reached end of mod list. Press any key to continue with server startup...");
                Console.ReadKey();
            }
            ProcessManager.StartServer();
            #endregion
        }
        #endregion

        private static void HandleCommands()
        {
            while (true)
            {
                var input = Console.ReadLine();
#if HcBuild
                switch (input.ToLower())
                {
                    case "restart":
                        ProcessManager.restartProcess = true;
                        break;
                    case "shutdown":
                        ProcessManager.shutdownProcess = true;
                        break;
                }
#endif

                ProcessManager.WriteInput(input);
            }
        }
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine(e.ExceptionObject.ToString());
            Console.ReadKey();
            Environment.Exit(1);
        }

        public static void ClearCache()
        {
            #region Clear Cache
            Logger.WriteConsole(Logger.LogType.Info, "Clearing cache...");
            if (!Directory.Exists("user/cache")) return;
            try
            {
                Directory.Delete("user/cache", true);
            }
            catch (Exception e)
            {
                Logger.WriteConsole(Logger.LogType.Error, "Error deleting the server cache: " + e.Message);
            }
            #endregion
        }
    }
}

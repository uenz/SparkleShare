//   SparkleShare, a collaboration and sharing tool.
//   Copyright (C) 2010  Hylke Bons <hi@planetpeanut.uk>
//
//   This program is free software: you can redistribute it and/or modify
//   it under the terms of the GNU General Public License as published by
//   the Free Software Foundation, either version 3 of the License, or
//   (at your option) any later version.
//
//   This program is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//   GNU General Public License for more details.
//
//   You should have received a copy of the GNU General Public License
//   along with this program. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Input.Platform;
using Avalonia.Platform;
using Sparkles;
using Sparkles.Git;
using SparkleShare.UserInterface;

namespace SparkleShare
{
    public class Controller : BaseController
    {
        public Controller(Configuration config)
            : base(config)
        {
        }

        public override string PresetsPath
        {
            get
            {
                return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty, "Presets");
            }
        }

        public override void Initialize()
        {
            string[] search_path= Array.Empty<string>();
            
            if (InstallationInfo.OperatingSystem == OS.Windows)
            {
                search_path = new string[] {
                    Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty, "git_scm", "mingw64", "bin"),
                    Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty, "git_scm", "mingw32", "bin"),
                    Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty, "git_scm", "usr", "bin"),
                    Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty, "git_scm", "cmd")
                };
                
                Environment.SetEnvironmentVariable("HOME", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            } else if (InstallationInfo.OperatingSystem == OS.macOS)
            {
                var exePath = Environment.ProcessPath ?? Assembly.GetExecutingAssembly().Location;
                var exeDir = Path.GetDirectoryName(exePath) ?? string.Empty;
                var resourcePath = Path.GetFullPath(Path.Combine(exeDir, "..", "Resources"));

                search_path = new string[] {
                    Path.Combine(resourcePath, "Resources", "git", "libexec", "git-core"), //debugging
                    Path.Combine(resourcePath, "..", "Resources", "git", "libexec", "git-core") //app bundle
                }; 
            }
            Command.SetSearchPath(search_path);
            //TODO: Check if necessary to set PATH as well, or if Command.SetSearchPath is sufficient
            Environment.SetEnvironmentVariable("HOME", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            base.Initialize();
        }
        public override string EventLogHTML
        {
            get
            {
                string html = UserInterfaceHelpers.GetHTML("event-log.html");
                return html.Replace("<!-- $jquery -->", UserInterfaceHelpers.GetHTML("jquery.js"));
            }
        }

        public override string DayEntryHTML
        {
            get
            {
                return UserInterfaceHelpers.GetHTML("day-entry.html");
            }
        }

        public override string EventEntryHTML
        {
            get
            {
                return UserInterfaceHelpers.GetHTML("event-entry.html");
            }
        }

        public override void SetFolderIcon()
        {
            if (InstallationInfo.OperatingSystem == OS.Windows)
            {
                SetFolderIconWindows();
            }
            else if (InstallationInfo.OperatingSystem == OS.macOS)
            {
                SetFolderIconMacOS();
            }
            else if (InstallationInfo.OperatingSystem == OS.Ubuntu ||
                     InstallationInfo.OperatingSystem == OS.GNOME)
            {
                SetFolderIconLinux();
            }
        }

        private void SetFolderIconWindows()
        {
            string app_path = Path.GetDirectoryName(Environment.ProcessPath ?? string.Empty) ?? string.Empty;
            string icon_file_path = Path.Combine(app_path, "Images", "sparkleshare-folder.ico");

            if (File.Exists(icon_file_path))
            {
                string ini_file_path = Path.Combine(FoldersPath, "desktop.ini");
                if (!File.Exists(ini_file_path))
                {
                    string n = Environment.NewLine;

                    string ini_file = "[.ShellClassInfo]" + n +
                        "IconFile=" + icon_file_path + n +
                        "IconIndex=0" + n +
                        "InfoTip=SparkleShare";

                    try
                    {
                        File.Create(ini_file_path).Close();
                        File.WriteAllText(ini_file_path, ini_file);

                        File.SetAttributes(ini_file_path,
                            File.GetAttributes(ini_file_path) | FileAttributes.Hidden | FileAttributes.System);
                    }
                    catch (IOException e)
                    {
                        Logger.LogInfo("Config", "Failed setting icon for '" + FoldersPath + "': " + e.Message);
                    }
                }
            }
        }

        private void SetFolderIconMacOS()
        {
            string folder_icon_name = "sparkleshare-folder.icns";

            if (Environment.OSVersion.Version.Major >= 14)
                folder_icon_name = "sparkleshare-folder-yosemite.icns";

            string app_path = Path.GetDirectoryName(Environment.ProcessPath ?? string.Empty) ?? string.Empty;
            string candidate1 = Path.Combine(app_path, "..", "Resources", folder_icon_name);
            string candidate2 = Path.Combine(app_path, folder_icon_name);
            string icon_file_path = Path.GetFullPath(File.Exists(candidate1) ? candidate1 : candidate2);

            if (!File.Exists(icon_file_path))
            {
                Logger.LogInfo("Config", "macOS folder icon file not found: " + folder_icon_name);
                return;
            }

            try
            {
                string script = "tell application \"Finder\"\n" +
                    "set icon of folder (POSIX file \"" + FoldersPath + "\") to POSIX file \"" + icon_file_path + "\"\n" +
                    "end tell";

                var psi = new ProcessStartInfo
                {
                    FileName = "/usr/bin/osascript",
                    Arguments = $"-e \"{script.Replace("\"", "\\\"")}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process != null)
                {
                    process.WaitForExit();
                    if (process.ExitCode != 0)
                    {
                        string error = process.StandardError.ReadToEnd();
                        Logger.LogInfo("Config", "Failed setting macOS folder icon: " + error);
                    }
                    else
                    {
                        Logger.LogInfo("Config", "Set macOS folder icon for " + FoldersPath);
                    }
                }
                else
                {
                    Logger.LogInfo("Config", "Failed to start osascript for setting folder icon");
                }
            }
            catch (Exception e)
            {
                Logger.LogInfo("Config", "Exception setting macOS folder icon: " + e.Message, e);
            }
        }

        private void SetFolderIconLinux()
        {
            try
            {
                string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string xdgDataHome = Path.Combine(home, ".local", "share");

                var psi = new ProcessStartInfo
                {
                    FileName = "/usr/bin/gio",
                    Arguments = $"set \"{FoldersPath}\" metadata::custom-icon-name org.sparkleshare.SparkleShare",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                psi.Environment["XDG_DATA_HOME"] = xdgDataHome;

                using var process = Process.Start(psi);
                if (process != null)
                {
                    process.WaitForExit();
                    if (process.ExitCode != 0)
                    {
                        string error = process.StandardError.ReadToEnd();
                        Logger.LogInfo("Config", "Failed setting Linux folder icon: " + error);
                    }
                    else
                    {
                        Logger.LogInfo("Config", "Set Linux folder icon for " + FoldersPath);
                    }
                }
                else
                {
                    Logger.LogInfo("Config", "Failed to start gio for setting folder icon");
                }
            }
            catch (Exception e)
            {
                Logger.LogInfo("Config", "Exception setting Linux folder icon: " + e.Message, e);
            }
        }

        public override void CreateStartupItem()
        {
            if (InstallationInfo.OperatingSystem == OS.Windows)
            {
                CreateStartupItemWindows();
            }
            else if (InstallationInfo.OperatingSystem == OS.macOS)
            {
                CreateStartupItemMacOS();
            }
            else if (InstallationInfo.OperatingSystem == OS.Ubuntu || InstallationInfo.OperatingSystem == OS.GNOME)
            {
                CreateStartupItemLinux();
            }
        }

        private void CreateStartupItemWindows()
        {
            string startup_folder_path = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            string shortcut_path = Path.Combine(startup_folder_path, "SparkleShare.lnk");

            if (File.Exists(shortcut_path))
                File.Delete(shortcut_path);

            string shortcut_target = Environment.ProcessPath ?? string.Empty;

            UserInterface.Shortcut shortcut = new UserInterface.Shortcut();
            shortcut.Create(shortcut_path, shortcut_target);
        }

        private void CreateStartupItemMacOS()
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string launch_agents_dir = Path.Combine(home, "Library", "LaunchAgents");
            string plist_path = Path.Combine(launch_agents_dir, "com.sparkleshare.SparkleShare.plist");

            if (!Directory.Exists(launch_agents_dir))
                Directory.CreateDirectory(launch_agents_dir);

            if (File.Exists(plist_path))
                return;

            string processPath = Environment.ProcessPath ?? string.Empty;
            if (string.IsNullOrWhiteSpace(processPath))
            {
                Logger.LogInfo("Config", "Could not create macOS startup item: process path is empty");
                return;
            }

            string[] programArguments;

            try
            {
                var exeFile = new FileInfo(processPath);
                if (exeFile.Directory?.Parent?.Parent != null && exeFile.Directory.Parent.Parent.Extension == ".app")
                {
                    string appBundlePath = exeFile.Directory.Parent.Parent.FullName;
                    programArguments = new[] { "/usr/bin/open", "-a", appBundlePath };
                }
                else
                {
                    programArguments = new[] { processPath };
                }
            }
            catch
            {
                programArguments = new[] { processPath };
            }

            string plist = $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
                "<!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">\n" +
                "<plist version=\"1.0\">\n" +
                "<dict>\n" +
                "  <key>Label</key>\n" +
                "  <string>com.sparkleshare.SparkleShare</string>\n" +
                "  <key>ProgramArguments</key>\n" +
                "  <array>\n" +
                string.Join("", Array.ConvertAll(programArguments, arg => "    <string>" + System.Security.SecurityElement.Escape(arg) + "</string>\n")) +
                "  </array>\n" +
                "  <key>RunAtLoad</key>\n" +
                "  <true/>\n" +
                "  <key>KeepAlive</key>\n" +
                "  <false/>\n" +
                "</dict>\n" +
                "</plist>\n";

            try
            {
                File.WriteAllText(plist_path, plist);
                Logger.LogInfo("Config", "Created macOS startup item: " + plist_path);
            }
            catch (Exception e)
            {
                Logger.LogInfo("Config", "Failed creating macOS startup item", e);
            }
        }

        private void CreateStartupItemLinux()
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string autostart_dir = Path.Combine(home, ".config", "autostart");
            string desktop_file_path = Path.Combine(autostart_dir, "SparkleShare.desktop");

            if (!Directory.Exists(autostart_dir))
                Directory.CreateDirectory(autostart_dir);

            if (File.Exists(desktop_file_path))
                return;

            string processPath = Environment.ProcessPath ?? string.Empty;
            if (string.IsNullOrWhiteSpace(processPath))
            {
                Logger.LogInfo("Config", "Could not create Linux startup item: process path is empty");
                return;
            }

            string execPath = processPath.Replace(" ", "\\ ");
            string desktopEntry = "[Desktop Entry]" + Environment.NewLine +
                "Type=Application" + Environment.NewLine +
                "Name=SparkleShare" + Environment.NewLine +
                "Exec=" + execPath + Environment.NewLine +
                "Terminal=false" + Environment.NewLine +
                "Hidden=false" + Environment.NewLine +
                "X-GNOME-Autostart-enabled=true" + Environment.NewLine +
                "NoDisplay=false" + Environment.NewLine;

            try
            {
                File.WriteAllText(desktop_file_path, desktopEntry);
                Logger.LogInfo("Config", "Created Linux startup item: " + desktop_file_path);
            }
            catch (Exception e)
            {
                Logger.LogInfo("Config", "Failed creating Linux startup item", e);
            }
        }

        public override void InstallProtocolHandler()
        {
            // Protocol handler installation is platform-specific
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // We ship a separate .exe for this on Windows
            }
        }

        public void AddToBookmarks()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                string user_profile_path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string shortcut_path = Path.Combine(user_profile_path, "Links", "SparkleShare.lnk");

                if (File.Exists(shortcut_path))
                    File.Delete(shortcut_path);

                UserInterface.Shortcut shortcut = new UserInterface.Shortcut();
                shortcut.Create(FoldersPath, shortcut_path);
            }
        }

        public override void CreateSparkleShareFolder()
        {
            if (!Directory.Exists(FoldersPath))
            {
                Directory.CreateDirectory(FoldersPath);

                if (InstallationInfo.OperatingSystem == OS.Windows)
                {
                    File.SetAttributes(FoldersPath, File.GetAttributes(FoldersPath) | FileAttributes.System);
                }

                Logger.LogInfo("Config", "Created '" + FoldersPath + "'");
            }
        }

        public override void OpenFile(string path)
        {
            var psi = new ProcessStartInfo(path)
            {
                UseShellExecute = true
            };
            Process.Start(psi);
        }

        public override void OpenFolder(string path)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = path,
                WorkingDirectory = path,
                UseShellExecute = true
            };
            Process.Start(startInfo);
        }

        public override void OpenWebsite(string url)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            };
            Process.Start(startInfo);
        }

        public override void CopyToClipboard(string text)
        {
            try
            {
                // Try to get the clipboard from the application
                var lifetime = Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
                
                IClipboard? clipboard = null;
                
                // First try: Get from main window
                if (lifetime?.MainWindow != null)
                {
                    clipboard = lifetime.MainWindow.Clipboard;
                }
                
                // Second try: Get from any open window
                if (clipboard == null && lifetime?.Windows != null)
                {
                    foreach (var window in lifetime.Windows)
                    {
                        if (window.Clipboard != null)
                        {
                            clipboard = window.Clipboard;
                            break;
                        }
                    }
                }
                
                // Third try: Use TopLevel service
                if (clipboard == null)
                {
                    var topLevel = Avalonia.Controls.TopLevel.GetTopLevel(lifetime?.MainWindow);
                    if (topLevel != null)
                    {
                        clipboard = topLevel.Clipboard;
                    }
                }
                
                if (clipboard != null)
                {
                    var task = clipboard.SetTextAsync(text);
                    task.Wait(TimeSpan.FromSeconds(2)); // Timeout after 2 seconds
                    Logger.LogInfo("Controller", "Text copied to clipboard successfully: " + text.Substring(0, Math.Min(20, text.Length)) + "...");
                }
                else
                {
                    Logger.LogInfo("Controller", "No clipboard service available - falling back to platform-specific method");
                    CopyToClipboardFallback(text);
                }
            }
            catch (Exception e)
            {
                Logger.LogInfo("Controller", "Copy to clipboard failed", e);
                CopyToClipboardFallback(text);
            }
        }

        private void CopyToClipboardFallback(string text)
        {
            try
            {
                if (InstallationInfo.OperatingSystem == OS.Windows)
                {
                    // Windows-specific clipboard using Win32 API
                    WindowsClipboard.SetText(text);
                    Logger.LogInfo("Controller", "Text copied using Windows clipboard API");
                }
                else if (InstallationInfo.OperatingSystem == OS.macOS)
                {
                    // macOS clipboard using pbcopy
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "pbcopy",
                            RedirectStandardInput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };
                    process.Start();
                    process.StandardInput.Write(text);
                    process.StandardInput.Close();
                    process.WaitForExit();
                    Logger.LogInfo("Controller", "Text copied using pbcopy");
                }
                else
                {
                    // Linux clipboard using xclip
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "xclip",
                            Arguments = "-selection clipboard",
                            RedirectStandardInput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };
                    process.Start();
                    process.StandardInput.Write(text);
                    process.StandardInput.Close();
                    process.WaitForExit();
                    Logger.LogInfo("Controller", "Text copied using xclip");
                }
            }
            catch (Exception ex)
            {
                Logger.LogInfo("Controller", "Fallback clipboard method also failed", ex);
            }
        }

        public override void PlatformQuit()
        {
            Environment.Exit(0);
        }
    }

    // Windows Clipboard helper using P/Invoke
    internal static class WindowsClipboard
    {
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseClipboard();

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EmptyClipboard();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalUnlock(IntPtr hMem);

        private const uint CF_UNICODETEXT = 13;
        private const uint GMEM_MOVEABLE = 0x0002;

        public static void SetText(string text)
        {
            if (!OpenClipboard(IntPtr.Zero))
                throw new Exception("Failed to open clipboard");

            try
            {
                EmptyClipboard();

                var bytes = (text.Length + 1) * 2;
                var hGlobal = GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)bytes);

                if (hGlobal == IntPtr.Zero)
                    throw new Exception("Failed to allocate memory");

                var target = GlobalLock(hGlobal);

                if (target == IntPtr.Zero)
                    throw new Exception("Failed to lock memory");

                try
                {
                    Marshal.Copy(text.ToCharArray(), 0, target, text.Length);
                }
                finally
                {
                    GlobalUnlock(hGlobal);
                }

                if (SetClipboardData(CF_UNICODETEXT, hGlobal) == IntPtr.Zero)
                    throw new Exception("Failed to set clipboard data");
            }
            finally
            {
                CloseClipboard();
            }
        }
    }
}

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

using Sparkles;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace SparkleShare.UserInterface
{
    public class Shortcut
    {
        public void Create(string target_path, string shortcut_path)
        {
            if (InstallationInfo.OperatingSystem == OS.Windows)
            {
                CreateWindowsShortcut(target_path, shortcut_path);
            }
            else if (InstallationInfo.OperatingSystem == OS.macOS)
            {
                CreateMacOSShortcut(target_path, shortcut_path);
            }
            else
            {
                CreateLinuxShortcut(target_path, shortcut_path);
            }
        }

        private void CreateWindowsShortcut(string target_path, string shortcut_path)
        {
            try
            {
                // Use COM IWshRuntimeLibrary if available at runtime
                Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null)
                {
                    Sparkles.Logger.LogInfo("Shortcut", "WScript.Shell COM not available");
                    return;
                }

                object? shellObj = Activator.CreateInstance(shellType);
                if (shellObj == null)
                {
                    Sparkles.Logger.LogInfo("Shortcut", "Failed to create WScript.Shell instance");
                    return;
                }

                dynamic shell = shellObj;
                dynamic lnk = shell.CreateShortcut(shortcut_path);
                if (lnk == null)
                {
                    Sparkles.Logger.LogInfo("Shortcut", "WScript.Shell.CreateShortcut returned null");
                    return;
                }

                lnk.TargetPath = target_path;
                lnk.WorkingDirectory = Path.GetDirectoryName(target_path);
                lnk.Save();

                Sparkles.Logger.LogInfo("Shortcut", "Created Windows shortcut: " + shortcut_path);
            }
            catch (Exception ex)
            {
                Sparkles.Logger.LogInfo("Shortcut", "Failed creating Windows shortcut", ex);
            }
        }

        private void CreateMacOSShortcut(string target_path, string shortcut_path)
        {
            try
            {
                // Creating a proper Finder alias is non-trivial; create a symlink as a pragmatic fallback
                if (Directory.Exists(shortcut_path))
                    Directory.Delete(shortcut_path);
                else if (File.Exists(shortcut_path))
                    File.Delete(shortcut_path);

                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "/bin/ln",
                    Arguments = $"-s \"{target_path}\" \"{shortcut_path}\"",
                    RedirectStandardOutput = false,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                var p = System.Diagnostics.Process.Start(psi);
                p?.WaitForExit();

                Sparkles.Logger.LogInfo("Shortcut", "Created macOS symlink: " + shortcut_path);
            }
            catch (Exception ex)
            {
                Sparkles.Logger.LogInfo("Shortcut", "Failed creating macOS symlink", ex);
            }
        }

        private void CreateLinuxShortcut(string target_path, string shortcut_path)
        {
            try
            {
                // Create a .desktop file pointing to the target_path
                var desktopEntry = new System.Text.StringBuilder();
                desktopEntry.AppendLine("[Desktop Entry]");
                desktopEntry.AppendLine("Type=Application");
                desktopEntry.AppendLine("Name=SparkleShare");
                desktopEntry.AppendLine($"Exec=xdg-open \"{target_path}\"");
                desktopEntry.AppendLine("Terminal=false");
                desktopEntry.AppendLine($"Icon=folder");

                File.WriteAllText(shortcut_path, desktopEntry.ToString());
                // Make it executable
                try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("/bin/chmod", $"+x \"{shortcut_path}\"") { UseShellExecute = false })?.WaitForExit(); } catch {}

                Sparkles.Logger.LogInfo("Shortcut", "Created Linux .desktop shortcut: " + shortcut_path);
            }
            catch (Exception ex)
            {
                Sparkles.Logger.LogInfo("Shortcut", "Failed creating Linux shortcut", ex);
            }
        }
    }
}

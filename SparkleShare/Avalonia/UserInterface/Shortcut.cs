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
using System.Runtime.InteropServices;

namespace SparkleShare.UserInterface
{
    public class Shortcut
    {
        public void Create(string target_path, string shortcut_path)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                CreateWindowsShortcut(target_path, shortcut_path);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                CreateMacOSShortcut(target_path, shortcut_path);
            }
        }

        private void CreateWindowsShortcut(string target_path, string shortcut_path)
        {
            // TODO: Implement Windows shortcut creation
            // This was previously done using IWshRuntimeLibrary
            // Need to find cross-platform alternative or keep Windows-specific implementation
            Sparkles.Logger.LogInfo("Shortcut", "Windows shortcut creation not yet implemented in Avalonia version");
        }

        private void CreateMacOSShortcut(string target_path, string shortcut_path)
        {
            // TODO: Implement macOS alias creation
            Sparkles.Logger.LogInfo("Shortcut", "macOS shortcut creation not yet implemented");
        }
    }
}

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
using System.IO;
using System.Reflection;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace SparkleShare.UserInterface
{
    public static class UserInterfaceHelpers
    {
        public static string ToHex(this System.Drawing.Color color)
        {
            return string.Format("#{0:X2}{1:X2}{2:X2}", color.R, color.G, color.B);
        }

        public static Bitmap? GetImageSource(string name)
        {
            return GetImageSource(name, "png");
        }

        public static Bitmap? GetImageSource(string name, string type)
        {
            try
            {
                // First try: load from file system (since images are copied with CopyToOutputDirectory)
                Assembly assembly = Assembly.GetExecutingAssembly();
                string appDir = Path.GetDirectoryName(assembly.Location) ?? string.Empty;
                string imagePath = Path.Combine(appDir, "Images", $"{name}.{type}");
                
                Sparkles.Logger.LogInfo("UserInterfaceHelpers", $"Looking for image at: {imagePath}");
                
                if (File.Exists(imagePath))
                {
                    Sparkles.Logger.LogInfo("UserInterfaceHelpers", $"Found image file: {imagePath}");
                    return new Bitmap(imagePath);
                }
                
                // Second try: embedded resources (legacy support)
                string resourceName = $"SparkleShare.Avalonia.Images.{name}.{type}";
                Stream? image_stream = assembly.GetManifestResourceStream(resourceName);
                
                if (image_stream != null)
                {
                    Sparkles.Logger.LogInfo("UserInterfaceHelpers", $"Found embedded resource: {resourceName}");
                    return new Bitmap(image_stream);
                }
                
                // Third try: look in parent Windows folder (for shared images)
                string windowsImagePath = Path.Combine(appDir, "..", "..", "SparkleShare", "Windows", "Images", $"{name}.{type}");
                windowsImagePath = Path.GetFullPath(windowsImagePath);
                
                if (File.Exists(windowsImagePath))
                {
                    Sparkles.Logger.LogInfo("UserInterfaceHelpers", $"Found Windows image: {windowsImagePath}");
                    return new Bitmap(windowsImagePath);
                }
                
                Sparkles.Logger.LogInfo("UserInterfaceHelpers", $"Image '{name}.{type}' not found in any location");
                return null;
            }
            catch (Exception ex)
            {
                Sparkles.Logger.LogInfo("UserInterfaceHelpers", $"Failed to load image '{name}.{type}': {ex.Message}");
                return null;
            }
        }

        public static Bitmap? GetImage(string absolutePath)
        {
            try
            {
                if (File.Exists(absolutePath))
                {
                    return new Bitmap(absolutePath);
                }
                return null;
            }
            catch (Exception ex)
            {
                Sparkles.Logger.LogInfo("UserInterfaceHelpers", $"Failed to load image from path '{absolutePath}': {ex.Message}");
                return null;
            }
        }

        public static Bitmap? GetBitmap(string name)
        {
            // Return Avalonia Bitmap instead of System.Drawing.Bitmap
            return GetImageSource(name, "png");
        }

        public static string GetHTML(string name)
        {
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                string resourceName = $"SparkleShare.Avalonia.HTML.{name}";
                
                using StreamReader? html_reader = new StreamReader(
                    assembly.GetManifestResourceStream(resourceName) 
                    ?? throw new FileNotFoundException($"HTML resource '{resourceName}' not found"));
                
                return html_reader.ReadToEnd();
            }
            catch (Exception ex)
            {
                Sparkles.Logger.LogInfo("UserInterfaceHelpers", $"Failed to load HTML '{name}': {ex.Message}");
                return string.Empty;
            }
        }

        public static Color ToAvaloniaColor(this System.Drawing.Color color)
        {
            return Color.FromArgb(color.A, color.R, color.G, color.B);
        }

        public static System.Drawing.Color ToDrawingColor(this Color color)
        {
            return System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
        }
    }
}

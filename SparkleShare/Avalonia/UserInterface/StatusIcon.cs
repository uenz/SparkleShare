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
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Threading;
using Sparkles;

namespace SparkleShare.UserInterface
{
    public class StatusIcon : IStatusIcon
    {
        public StatusIconController Controller = new StatusIconController();
        private TrayIcon? trayIcon;
        private NativeMenu? menu;

        public StatusIcon()
        {
            CreateTrayIcon();

            Controller.UpdateIconEvent += delegate (IconState state)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    UpdateIcon(state);
                });
            };

            Controller.UpdateMenuEvent += delegate
            {
                Dispatcher.UIThread.Post(() =>
                {
                    UpdateMenu();
                });
            };

            Controller.UpdateQuitItemEvent += delegate (bool quit_item_enabled)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    UpdateQuitItem(quit_item_enabled);
                });
            };

            Controller.UpdateStatusItemEvent += delegate (string state_text)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    UpdateStatusItem(state_text);
                });
            };
        }

        public void Dispose()
        {
            if (trayIcon != null)
            {
                trayIcon.IsVisible = false;
            }
        }

        public void ShowBalloon(string title, string message)
        {
            Sparkles.Logger.LogInfo("StatusIcon", $"Notification: {title} - {message}");
        }

        public void ShowBalloon(string title, string subtext, string image_path)
        {
            Sparkles.Logger.LogInfo("StatusIcon", $"Notification: {title} - {subtext}");
        }

        private void CreateTrayIcon()
        {
            try
            {
                Logger.LogInfo("StatusIcon", "Creating TrayIcon...");
                trayIcon = new TrayIcon();
                menu = new NativeMenu();
                
                trayIcon.Menu = menu;
                trayIcon.IsVisible = true;
                
                UpdateIcon(IconState.Idle);
                UpdateMenu();
                
                Logger.LogInfo("StatusIcon", "TrayIcon created successfully");
            }
            catch (Exception ex)
            {
                Logger.LogInfo("StatusIcon", "Failed to create TrayIcon", ex);
                throw;
            }
        }

        private void UpdateIcon(IconState state)
        {
            if (trayIcon == null) return;

            string icon_name = state switch
            {
                IconState.Syncing => "process-syncing",
                IconState.Error => "process-syncing-error",
                IconState.SyncingUp => "process-syncing-up",
                IconState.SyncingDown => "process-syncing-down",
                _ => "process-syncing-idle"
            };

            try
            {
                var avaloniaIcon = UserInterfaceHelpers.GetImageSource(icon_name);
                if (avaloniaIcon != null)
                {
                    trayIcon.Icon = new Avalonia.Controls.WindowIcon(avaloniaIcon);
                }
                else
                {
                    Logger.LogInfo("StatusIcon", $"Icon '{icon_name}' not found, using fallback");
                    var fallback = CreateFallbackIcon(state);
                    if (fallback != null)
                        trayIcon.Icon = new Avalonia.Controls.WindowIcon(fallback);
                }
            }
            catch (Exception ex)
            {
                Logger.LogInfo("StatusIcon", $"Error loading icon '{icon_name}'", ex);
            }

            string tooltip = state switch
            {
                IconState.Syncing => "Syncing...",
                IconState.Error => "Not everything synced",
                IconState.SyncingUp => "Sending changes...",
                IconState.SyncingDown => "Receiving changes...",
                _ => "Everything up to date"
            };

            trayIcon.ToolTipText = "SparkleShare\n" + tooltip;
        }

        private Avalonia.Media.Imaging.Bitmap? CreateFallbackIcon(IconState state)
        {
            try
            {
                var color = state switch
                {
                    IconState.Error => Avalonia.Media.Colors.Red,
                    IconState.Syncing => Avalonia.Media.Colors.Blue,
                    _ => Avalonia.Media.Colors.Green
                };

                var pixelSize = new Avalonia.PixelSize(16, 16);
                var dpi = new Avalonia.Vector(96, 96);
                var bitmap = new Avalonia.Media.Imaging.WriteableBitmap(
                    pixelSize, dpi, Avalonia.Platform.PixelFormat.Bgra8888, Avalonia.Platform.AlphaFormat.Premul);

                using (var fb = bitmap.Lock())
                {
                    unsafe
                    {
                        var ptr = (uint*)fb.Address.ToPointer();
                        uint colorValue = (uint)((color.A << 24) | (color.R << 16) | (color.G << 8) | color.B);
                        for (int i = 0; i < pixelSize.Width * pixelSize.Height; i++)
                            ptr[i] = colorValue;
                    }
                }

                return bitmap;
            }
            catch (Exception ex)
            {
                Logger.LogInfo("StatusIcon", "Failed to create fallback icon", ex);
                return null;
            }
        }

        private void UpdateMenu()
        {
            if (menu == null) return;

            menu.Items.Clear();

            // Project items
            if (Controller.Projects.Length > 0)
            {
                foreach (ProjectInfo project in Controller.Projects)
                {
                    var folder_item = new NativeMenuItem { Header = project.Name };
                    string project_name = project.Name;
                    folder_item.Click += (sender, e) => Controller.ProjectClicked(project_name);
                    menu.Items.Add(folder_item);
                }

                menu.Items.Add(new NativeMenuItemSeparator());
            }

            // Add hosted project
            var add_item = new NativeMenuItem { Header = "Add Hosted Project\u2026" };
            add_item.Click += (sender, e) =>
            {
                try { Controller.AddHostedProjectClicked(); }
                catch (Exception ex) { Logger.LogInfo("StatusIcon", "Error in AddHostedProjectClicked", ex); }
            };
            menu.Items.Add(add_item);

            // Recent changes
            var recent_changes_item = new NativeMenuItem
            {
                Header = "View Recent Changes\u2026",
                IsEnabled = Controller.RecentEventsItemEnabled
            };
            recent_changes_item.Click += (sender, e) =>
            {
                try { Controller.RecentEventsClicked(); }
                catch (Exception ex) { Logger.LogInfo("StatusIcon", "Error in RecentEventsClicked", ex); }
            };
            menu.Items.Add(recent_changes_item);

            menu.Items.Add(new NativeMenuItemSeparator());

            // About
            var about_item = new NativeMenuItem { Header = "About SparkleShare" };
            about_item.Click += (sender, e) =>
            {
                try { Controller.AboutClicked(); }
                catch (Exception ex) { Logger.LogInfo("StatusIcon", "Error in AboutClicked", ex); }
            };
            menu.Items.Add(about_item);

            menu.Items.Add(new NativeMenuItemSeparator());

            // Quit
            var quit_item = new NativeMenuItem
            {
                Header = "Quit",
                IsEnabled = Controller.QuitItemEnabled
            };
            quit_item.Click += (sender, e) => Controller.QuitClicked();
            menu.Items.Add(quit_item);
        }

        private void UpdateQuitItem(bool enabled)
        {
            if (menu == null || menu.Items.Count == 0) return;

            foreach (var item in menu.Items)
            {
                if (item is NativeMenuItem menuItem && menuItem.Header?.ToString() == "Quit")
                {
                    menuItem.IsEnabled = enabled;
                    break;
                }
            }
        }

        private void UpdateStatusItem(string state_text)
        {
            if (trayIcon != null)
            {
                trayIcon.ToolTipText = "SparkleShare\n" + state_text;
            }
        }
    }
}

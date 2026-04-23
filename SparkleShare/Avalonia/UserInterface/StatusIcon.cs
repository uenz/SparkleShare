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

            try
            {
                menu.Items.Clear();

            // State item (disabled)
            var state_item = new NativeMenuItem
            {
                Header = Controller.StateText,
                IsEnabled = false
            };
            menu.Items.Add(state_item);
            menu.Items.Add(new NativeMenuItemSeparator());

            // Main "SparkleShare" menu with submenu
            var sparkleShare_menu = new NativeMenu();

            // Recent changes
            var recent_changes_item = new NativeMenuItem
            {
                Header = "\uD83D\uDCC4 Recent changes\u2026"
            };
            recent_changes_item.Click += (sender, e) =>
            {
                try { Controller.RecentEventsClicked(); }
                catch (Exception ex) { Logger.LogInfo("StatusIcon", "Error in RecentEventsClicked", ex); }
            };
            sparkleShare_menu.Items.Add(recent_changes_item);

            // Add hosted project
            var add_item = new NativeMenuItem { Header = "\u2795 Add hosted project\u2026" };
            add_item.Click += (sender, e) =>
            {
                try { Controller.AddHostedProjectClicked(); }
                catch (Exception ex) { Logger.LogInfo("StatusIcon", "Error in AddHostedProjectClicked", ex); }
            };
            sparkleShare_menu.Items.Add(add_item);
            sparkleShare_menu.Items.Add(new NativeMenuItemSeparator());

            // Notifications toggle with checkbox
            var notify_item = new NativeMenuItem
            {
                Header = GetNotificationMenuText(),
                ToggleType = NativeMenuItemToggleType.CheckBox
            };
            
            // Set initial checked state
            try
            {
                notify_item.IsChecked = SparkleShare.Controller.NotificationsEnabled;
            }
            catch (Exception ex)
            {
                Logger.LogInfo("StatusIcon", "Could not set IsChecked, using text indicator only", ex);
            }
            
            notify_item.Click += (sender, e) =>
            {
                try
                {
                    SparkleShare.Controller.ToggleNotifications();
                    
                    // Update both the checkbox and the text
                    try
                    {
                        notify_item.IsChecked = SparkleShare.Controller.NotificationsEnabled;
                    }
                    catch { }
                    
                    notify_item.Header = GetNotificationMenuText();
                }
                catch (Exception ex) { Logger.LogInfo("StatusIcon", "Error toggling notifications", ex); }
            };
            sparkleShare_menu.Items.Add(notify_item);
            sparkleShare_menu.Items.Add(new NativeMenuItemSeparator());

            // Client ID submenu
            if (Controller.LinkCodeItemEnabled && SparkleShare.Controller.UserAuthenticationInfo?.PublicKey != null)
            {
                var link_code_menu = new NativeMenu();
                
                string publicKey = SparkleShare.Controller.UserAuthenticationInfo.PublicKey;
                var code_item = new NativeMenuItem
                {
                    Header = publicKey.Length > 20 ? publicKey.Substring(0, 20) + "..." : publicKey,
                    IsEnabled = false
                };
                link_code_menu.Items.Add(code_item);
                link_code_menu.Items.Add(new NativeMenuItemSeparator());
                
                var copy_item = new NativeMenuItem { Header = "\uD83D\uDCCB Copy to Clipboard" };
                copy_item.Click += (sender, e) =>
                {
                    try { Controller.CopyToClipboardClicked(); }
                    catch (Exception ex) { Logger.LogInfo("StatusIcon", "Error copying to clipboard", ex); }
                };
                link_code_menu.Items.Add(copy_item);

                var link_code_item = new NativeMenuItem
                {
                    Header = "\uD83D\uDD11 Client ID",
                    Menu = link_code_menu
                };
                sparkleShare_menu.Items.Add(link_code_item);
                sparkleShare_menu.Items.Add(new NativeMenuItemSeparator());
            }

            // About
            var about_item = new NativeMenuItem { Header = "\u2139\uFE0F About SparkleShare" };
            about_item.Click += (sender, e) =>
            {
                try { Controller.AboutClicked(); }
                catch (Exception ex) { Logger.LogInfo("StatusIcon", "Error in AboutClicked", ex); }
            };
            sparkleShare_menu.Items.Add(about_item);

            var sparkleShare_item = new NativeMenuItem
            {
                Header = "\uD83D\uDCC2 SparkleShare",
                Menu = sparkleShare_menu
            };
            menu.Items.Add(sparkleShare_item);

            // Project items with submenus
            if (Controller.Projects.Length > 0)
            {
                foreach (ProjectInfo project in Controller.Projects)
                {
                    var project_menu = new NativeMenu();
                    string project_name = project.Name;

                    // Status message (disabled)
                    var project_state_item = new NativeMenuItem
                    {
                        Header = project.StatusMessage,
                        IsEnabled = false
                    };
                    project_menu.Items.Add(project_state_item);
                    project_menu.Items.Add(new NativeMenuItemSeparator());

                    // Open folder
                    var open_item = new NativeMenuItem { Header = "\uD83D\uDCC2 Open folder" };
                    open_item.Click += Controller.OpenFolderDelegate(project_name);
                    project_menu.Items.Add(open_item);
                    project_menu.Items.Add(new NativeMenuItemSeparator());

                    if (project.IsPaused)
                    {
                        // Show unsynced changes if any
                        if (project.UnsyncedChangesInfo.Count > 0)
                        {
                            foreach (KeyValuePair<string, string> pair in project.UnsyncedChangesInfo)
                            {
                                project_menu.Items.Add(new NativeMenuItem
                                {
                                    Header = pair.Key,
                                    IsEnabled = false
                                });
                            }

                            if (!string.IsNullOrEmpty(project.MoreUnsyncedChanges))
                            {
                                project_menu.Items.Add(new NativeMenuItem
                                {
                                    Header = project.MoreUnsyncedChanges,
                                    IsEnabled = false
                                });
                            }

                            project_menu.Items.Add(new NativeMenuItemSeparator());

                            var resume_item = new NativeMenuItem { Header = "\u25B6\uFE0F Sync and Resume\u2026" };
                            resume_item.Click += (sender, e) => Controller.ResumeDelegate(project_name)(sender, e);
                            project_menu.Items.Add(resume_item);
                        }
                        else
                        {
                            var resume_item = new NativeMenuItem { Header = "\u25B6\uFE0F Resume" };
                            resume_item.Click += (sender, e) => Controller.ResumeDelegate(project_name)(sender, e);
                            project_menu.Items.Add(resume_item);
                        }
                    }
                    else
                    {
                        if (project.HasError)
                        {
                            var try_again_item = new NativeMenuItem { Header = "\u26A0\uFE0F Retry Sync" };
                            try_again_item.Click += (sender, e) => Controller.TryAgainDelegate(project_name)(sender, e);
                            project_menu.Items.Add(try_again_item);
                        }
                        else
                        {
                            var pause_item = new NativeMenuItem { Header = "\u23F8\uFE0F Pause" };
                            pause_item.Click += (sender, e) => Controller.PauseDelegate(project_name)(sender, e);
                            project_menu.Items.Add(pause_item);
                        }
                    }

                    string folder_icon = project.HasError ? "\u26A0\uFE0F" : "\uD83D\uDCC1";
                    var folder_item = new NativeMenuItem
                    {
                        Header = folder_icon + " " + project.Name.Replace("_", "__"),
                        Menu = project_menu
                    };
                    menu.Items.Add(folder_item);
                }
            }

            menu.Items.Add(new NativeMenuItemSeparator());

            // Quit - always visible, header shows sync state
            var quit_item = new NativeMenuItem
            {
                Header = Controller.QuitItemEnabled ? "\u274C Exit" : "\u274C Exit (syncing...)"
            };
            quit_item.Click += (sender, e) =>
            {
                if (Controller.QuitItemEnabled) Controller.QuitClicked();
            };
            menu.Items.Add(quit_item);
            }
            catch (Exception ex)
            {
                Logger.LogInfo("StatusIcon", "Error building menu", ex);
            }
        }

        private void UpdateQuitItem(bool enabled)
        {
            if (menu == null || menu.Items.Count == 0) return;

            foreach (var item in menu.Items)
            {
                if (item is NativeMenuItem menuItem &&
                    menuItem.Header?.ToString()?.Contains("Exit") == true)
                {
                    menuItem.Header = enabled ? "\u274C Exit" : "\u274C Exit (syncing...)";
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

            // Update state item in menu
            if (menu != null && menu.Items.Count > 0)
            {
                if (menu.Items[0] is NativeMenuItem stateItem)
                {
                    stateItem.Header = state_text;
                }

                // Update project status messages
                int projectIndex = 0;
                foreach (var item in menu.Items)
                {
                    if (item is NativeMenuItem menuItem && menuItem.Menu != null)
                    {
                        // Skip the "SparkleShare" main menu
                        if (menuItem.Header?.ToString() == "SparkleShare")
                            continue;

                        // This is a project menu
                        if (projectIndex < Controller.Projects.Length && menuItem.Menu.Items.Count > 0)
                        {
                            if (menuItem.Menu.Items[0] is NativeMenuItem projectStateItem)
                            {
                                projectStateItem.Header = Controller.Projects[projectIndex].StatusMessage;
                            }
                            projectIndex++;
                        }
                    }
                }
            }
        }

        private string GetNotificationMenuText()
        {
            // Windows-kompatible Unicode-Zeichen
            // Diese Zeichen werden in den meisten Windows-Schriftarten unterstützt
            if (SparkleShare.Controller.NotificationsEnabled)
                return "☑ Notifications";  // U+2611 Ballot Box with Check
            else
                return "☐ Notifications";  // U+2610 Ballot Box
        }
    }
}

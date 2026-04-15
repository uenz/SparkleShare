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
using System.Collections.ObjectModel;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace SparkleShare.UserInterface
{
    public class EventLog : Window, IEventLog
    {
        public EventLogController Controller { get; } = new EventLogController();

        private TextBlock  _sizeLabel    = null!;
        private TextBlock  _historyLabel = null!;
        private ComboBox   _comboBox     = null!;
        private ScrollViewer _scrollViewer = null!;
        private StackPanel _entriesPanel  = null!;
        private Panel      _spinnerPanel  = null!;
        private TextBlock  _spinnerLabel  = null!;

        public EventLog()
        {
            Title  = "Recent Changes";
            Width  = 800;
            Height = 600;
            CanResize = true;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            var icon = UserInterfaceHelpers.GetImageSource("sparkleshare-app", "ico");
            if (icon != null) Icon = new WindowIcon(icon);

            BuildUI();
            BindController();
        }

        // ?? Layout ????????????????????????????????????????????????????????????
        private void BuildUI()
        {
            var root = new Grid();
            root.RowDefinitions.Add(new RowDefinition(new GridLength(36)));
            root.RowDefinitions.Add(new RowDefinition(new GridLength(1, GridUnitType.Star)));

            // ?? Top bar ??
            var top_bar = new Border
            {
                Background      = new SolidColorBrush(Color.FromRgb(240, 240, 240)),
                BorderBrush     = new SolidColorBrush(Color.FromRgb(223, 223, 223)),
                BorderThickness = new Thickness(0, 0, 0, 1)
            };

            var top_grid = new Grid { Margin = new Thickness(8, 0, 8, 0) };
            top_grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            top_grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            top_grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
            top_grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

            _sizeLabel = new TextBlock
            {
                Text = "Size: ?", FontWeight = FontWeight.Bold,
                VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 16, 0)
            };
            _historyLabel = new TextBlock
            {
                Text = "History: ?", FontWeight = FontWeight.Bold,
                VerticalAlignment = VerticalAlignment.Center
            };
            _comboBox = new ComboBox
            {
                MinWidth = 140,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            Grid.SetColumn(_sizeLabel,    0);
            Grid.SetColumn(_historyLabel, 1);
            Grid.SetColumn(_comboBox,     3);
            top_grid.Children.Add(_sizeLabel);
            top_grid.Children.Add(_historyLabel);
            top_grid.Children.Add(_comboBox);
            top_bar.Child = top_grid;
            Grid.SetRow(top_bar, 0);
            root.Children.Add(top_bar);

            // ?? Content area: spinner OR entries ??
            var content = new Grid();

            // Spinner (shown while loading)
            _spinnerPanel = new Panel
            {
                IsVisible = true,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment   = VerticalAlignment.Stretch
            };
            _spinnerLabel = new TextBlock
            {
                Text = "Loading…",
                FontSize = 16,
                Opacity  = 0.5,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment   = VerticalAlignment.Center
            };
            _spinnerPanel.Children.Add(_spinnerLabel);

            // Entries list
            _entriesPanel = new StackPanel { Margin = new Thickness(12), Spacing = 8 };
            _scrollViewer = new ScrollViewer
            {
                Content   = _entriesPanel,
                IsVisible = false,
                HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled
            };

            content.Children.Add(_spinnerPanel);
            content.Children.Add(_scrollViewer);
            Grid.SetRow(content, 1);
            root.Children.Add(content);

            Content = root;
        }

        // ?? Controller binding ????????????????????????????????????????????????
        private void BindController()
        {
            Controller.ShowWindowEvent += () => Dispatcher.UIThread.Post(() => { Show(); Activate(); });
            Controller.HideWindowEvent += () => Dispatcher.UIThread.Post(() =>
            {
                Hide();
                ShowSpinner();
            });

            Controller.UpdateSizeInfoEvent += (size, history) => Dispatcher.UIThread.Post(() =>
            {
                _sizeLabel.Text    = "Size: " + size;
                _historyLabel.Text = "History: " + history;
            });

            Controller.UpdateChooserEvent += folders => Dispatcher.UIThread.Post(() =>
                UpdateChooser(folders));

            Controller.UpdateChooserEnablementEvent += enabled =>
                Dispatcher.UIThread.Post(() => _comboBox.IsEnabled = enabled);

            Controller.ContentLoadingEvent += () => Dispatcher.UIThread.Post(ShowSpinner);

            Controller.UpdateContentEvent += html => Dispatcher.UIThread.Post(() =>
            {
                RenderHtmlAsNative(html);
                ShowContent();
            });

            Controller.ShowSaveDialogEvent += (file_name, target_folder) =>
                Dispatcher.UIThread.Post(async () =>
                {
                    var top = TopLevel.GetTopLevel(this);
                    if (top == null) { Controller.SaveDialogCancelled(); return; }

                    var options = new Avalonia.Platform.Storage.FilePickerSaveOptions
                    {
                        Title           = "Restore from History",
                        SuggestedFileName = file_name,
                        SuggestedStartLocation = await top.StorageProvider.TryGetFolderFromPathAsync(new Uri(target_folder))
                    };

                    var result = await top.StorageProvider.SaveFilePickerAsync(options);
                    if (result != null) Controller.SaveDialogCompleted(result.Path.LocalPath);
                    else                Controller.SaveDialogCancelled();
                });
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            e.Cancel = true;
            Controller.WindowClosed();
        }

        // ?? Chooser ???????????????????????????????????????????????????????????
        private void UpdateChooser(string[] folders)
        {
            _comboBox.SelectionChanged -= OnChooserChanged;
            _comboBox.Items.Clear();
            _comboBox.Items.Add(new ComboBoxItem { Content = "Summary" });

            if (folders != null)
            {
                foreach (var f in folders)
                    _comboBox.Items.Add(new ComboBoxItem { Content = f });
            }

            _comboBox.SelectedIndex = 0;

            if (Controller.SelectedFolder != null)
            {
                for (int i = 1; i < _comboBox.Items.Count; i++)
                {
                    if ((_comboBox.Items[i] as ComboBoxItem)?.Content?.ToString() == Controller.SelectedFolder)
                    {
                        _comboBox.SelectedIndex = i;
                        break;
                    }
                }
            }

            _comboBox.SelectionChanged += OnChooserChanged;
        }

        private void OnChooserChanged(object? sender, SelectionChangedEventArgs e)
        {
            int idx = _comboBox.SelectedIndex;
            Controller.SelectedFolder = idx == 0 ? null
                : (_comboBox.Items[idx] as ComboBoxItem)?.Content?.ToString() ?? null;
        }

        // ?? Native HTML renderer ??????????????????????????????????????????????
        // Parses the HTML that SparkleShare generates and renders it as
        // native Avalonia controls (no WebView dependency needed).
        private void RenderHtmlAsNative(string html)
        {
            _entriesPanel.Children.Clear();
            WriteOutImages();

            // Extract day-entry blocks from the HTML
            // The HTML structure is: <div class="day-entry">...</div> blocks
            int pos = 0;
            while (true)
            {
                int start = html.IndexOf("<div class=\"day-entry\">", pos, StringComparison.OrdinalIgnoreCase);
                if (start < 0) break;
                int end = html.IndexOf("</div>", start, StringComparison.OrdinalIgnoreCase);
                if (end < 0) break;

                string block = html.Substring(start, end - start + 6);
                RenderDayEntry(block);
                pos = end + 6;
            }

            // Fallback: if no structured entries found show plain message
            if (_entriesPanel.Children.Count == 0)
            {
                _entriesPanel.Children.Add(new TextBlock
                {
                    Text     = "No recent changes.",
                    Opacity  = 0.5,
                    FontSize = 14,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin   = new Thickness(0, 40, 0, 0)
                });
            }
        }

        private void RenderDayEntry(string block)
        {
            // Day header
            var header_start = block.IndexOf("<h2>", StringComparison.OrdinalIgnoreCase);
            var header_end   = block.IndexOf("</h2>", StringComparison.OrdinalIgnoreCase);

            if (header_start >= 0 && header_end > header_start)
            {
                string day_text = StripTags(block.Substring(header_start + 4, header_end - header_start - 4));

                var day_header = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)),
                    Padding    = new Thickness(8, 4),
                    Margin     = new Thickness(0, 8, 0, 2)
                };
                day_header.Child = new TextBlock
                {
                    Text       = day_text,
                    FontWeight = FontWeight.Bold,
                    FontSize   = 13
                };
                _entriesPanel.Children.Add(day_header);
            }

            // Event entries within the day block
            int pos = 0;
            while (true)
            {
                int entry_start = block.IndexOf("<tr", pos, StringComparison.OrdinalIgnoreCase);
                if (entry_start < 0) break;
                int entry_end = block.IndexOf("</tr>", entry_start, StringComparison.OrdinalIgnoreCase);
                if (entry_end < 0) break;

                string row = block.Substring(entry_start, entry_end - entry_start + 5);
                RenderEventRow(row);
                pos = entry_end + 5;
            }
        }

        private void RenderEventRow(string row)
        {
            // Extract user, action, file info from the <tr> block
            string user   = ExtractTagContent(row, "td", "who");
            string action = ExtractTagContent(row, "td", "what");
            string time   = ExtractTagContent(row, "td", "when");

            if (string.IsNullOrWhiteSpace(user) && string.IsNullOrWhiteSpace(action))
                return;

            var entry = new Grid { Margin = new Thickness(4, 2) };
            entry.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(32)));
            entry.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(140)));
            entry.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
            entry.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(70)));

            // Avatar placeholder
            var avatar = new Border
            {
                Width  = 24, Height = 24,
                CornerRadius = new CornerRadius(12),
                Background   = new SolidColorBrush(Color.FromRgb(160, 160, 160))
            };
            Grid.SetColumn(avatar, 0);
            entry.Children.Add(avatar);

            // User name
            var user_lbl = new TextBlock
            {
                Text = StripTags(user),
                FontWeight = FontWeight.Bold,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(user_lbl, 1);
            entry.Children.Add(user_lbl);

            // Action / file
            var action_lbl = new TextBlock
            {
                Text = StripTags(action),
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center,
                Opacity = 0.7
            };
            Grid.SetColumn(action_lbl, 2);
            entry.Children.Add(action_lbl);

            // Time
            var time_lbl = new TextBlock
            {
                Text = StripTags(time),
                FontSize  = 11,
                Opacity   = 0.5,
                TextAlignment = TextAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(time_lbl, 3);
            entry.Children.Add(time_lbl);

            _entriesPanel.Children.Add(entry);
        }

        // ?? Image export (same as Windows version) ???????????????????????????
        private void WriteOutImages()
        {
            string tmp_path     = Sparkles.Configuration.DefaultConfiguration.TmpPath;
            string pixmaps_path = Path.Combine(tmp_path, "Images");

            if (!Directory.Exists(pixmaps_path))
            {
                Directory.CreateDirectory(pixmaps_path);
                File.SetAttributes(tmp_path, File.GetAttributes(tmp_path) | FileAttributes.Hidden);
            }

            string[] actions = { "added", "deleted", "edited", "moved" };
            foreach (string action in actions)
            {
                string file_path = Path.Combine(pixmaps_path, $"document-{action}-12.png");
                if (File.Exists(file_path)) continue;

                var bmp = UserInterfaceHelpers.GetImageSource($"document-{action}-12");
                if (bmp == null) continue;

                using var stream = new FileStream(file_path, FileMode.Create);
                bmp.Save(stream);
            }
        }

        // ?? Helpers ???????????????????????????????????????????????????????????
        private void ShowSpinner()  { _spinnerPanel.IsVisible = true;  _scrollViewer.IsVisible = false; }
        private void ShowContent()  { _spinnerPanel.IsVisible = false; _scrollViewer.IsVisible = true;  }

        private static string StripTags(string html)
        {
            if (string.IsNullOrEmpty(html)) return "";
            // Remove all HTML tags
            var result = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", "");
            return System.Net.WebUtility.HtmlDecode(result).Trim();
        }

        private static string ExtractTagContent(string html, string tag, string cssClass)
        {
            string search = $"<{tag} class=\"{cssClass}\"";
            int start = html.IndexOf(search, StringComparison.OrdinalIgnoreCase);
            if (start < 0) return "";
            int content_start = html.IndexOf('>', start) + 1;
            int content_end   = html.IndexOf($"</{tag}>", content_start, StringComparison.OrdinalIgnoreCase);
            if (content_start <= 0 || content_end < 0) return "";
            return html.Substring(content_start, content_end - content_start);
        }
    }
}

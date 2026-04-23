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
                int start = html.IndexOf("class='day-entry'", pos, StringComparison.OrdinalIgnoreCase);
                if (start < 0) break;

                // Go back to find the opening <div
                int div_start = html.LastIndexOf("<div", start, StringComparison.OrdinalIgnoreCase);
                if (div_start < 0) break;

                // Find the matching closing </div> by counting nested divs
                int depth = 0;
                int search = div_start;
                int block_end = -1;
                while (search < html.Length)
                {
                    int next_open  = html.IndexOf("<div",  search, StringComparison.OrdinalIgnoreCase);
                    int next_close = html.IndexOf("</div>", search, StringComparison.OrdinalIgnoreCase);

                    if (next_close < 0) break;

                    if (next_open >= 0 && next_open < next_close)
                    {
                        depth++;
                        search = next_open + 4;
                    }
                    else
                    {
                        depth--;
                        search = next_close + 6;
                        if (depth == 0) { block_end = next_close + 6; break; }
                    }
                }

                if (block_end < 0) break;

                string block = html.Substring(div_start, block_end - div_start);
                RenderDayEntry(block);
                pos = block_end;
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
            string day_text = ExtractDivContent(block, "day-entry-header");
            if (!string.IsNullOrWhiteSpace(day_text))
            {
                var day_header = new Border
                {
                    Background      = new SolidColorBrush(Color.FromRgb(245, 245, 245)),
                    BorderBrush     = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                    BorderThickness = new Thickness(0, 0, 0, 1),
                    Padding         = new Thickness(8, 4),
                    Margin          = new Thickness(0, 8, 0, 4)
                };
                day_header.Child = new TextBlock
                {
                    Text       = StripTags(day_text),
                    FontWeight = FontWeight.Bold,
                    FontSize   = 13
                };
                _entriesPanel.Children.Add(day_header);
            }

            // Event entries within the day block
            int pos = 0;
            while (true)
            {
                int entry_start = block.IndexOf("class='event-entry'", pos, StringComparison.OrdinalIgnoreCase);
                if (entry_start < 0) break;

                int div_start = block.LastIndexOf("<div", entry_start, StringComparison.OrdinalIgnoreCase);
                if (div_start < 0) break;

                // Find matching closing </div>
                int depth = 0, search = div_start, entry_end = -1;
                while (search < block.Length)
                {
                    int next_open  = block.IndexOf("<div",  search, StringComparison.OrdinalIgnoreCase);
                    int next_close = block.IndexOf("</div>", search, StringComparison.OrdinalIgnoreCase);
                    if (next_close < 0) break;
                    if (next_open >= 0 && next_open < next_close) { depth++; search = next_open + 4; }
                    else { depth--; search = next_close + 6; if (depth == 0) { entry_end = next_close + 6; break; } }
                }

                if (entry_end < 0) break;

                string entry_block = block.Substring(div_start, entry_end - div_start);
                RenderEventEntry(entry_block);
                pos = entry_end;
            }
        }

        private void RenderEventEntry(string entry_block)
        {
            // Extract user, action, file info from the <tr> block
            string user_name = StripTags(ExtractDivContent(entry_block, "event-user-name"));

            // Extract event content: <!-- $event-entry-content --> is replaced with <dl>...</dl>
            string content_raw = ExtractDlContent(entry_block);

            // Extract avatar URL from style='background-image: url("...")'
            string avatar_url = ExtractAvatarUrl(entry_block);

            if (string.IsNullOrWhiteSpace(user_name) && string.IsNullOrWhiteSpace(content_raw))
                return;

            var entry_border = new Border
            {
                BorderBrush     = new SolidColorBrush(Color.FromRgb(230, 230, 230)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding         = new Thickness(4, 6),
            };

            var entry = new Grid();
            entry.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(36)));
            entry.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));

            // Avatar placeholder
            var avatar_border = new Border
            {
                Width = 28, Height = 28,
                CornerRadius = new CornerRadius(14),
                Background   = new SolidColorBrush(Color.FromRgb(180, 180, 180)),
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 2, 4, 0)
            };

            // User name
            if (!string.IsNullOrEmpty(avatar_url))
            {
                try
                {
                    string local_path = avatar_url.Replace("file://", "").Replace("/", System.IO.Path.DirectorySeparatorChar.ToString());
                    if (System.IO.File.Exists(local_path))
                    {
                        var bmp = new Bitmap(local_path);
                        avatar_border.Background = new ImageBrush(bmp) { Stretch = Stretch.UniformToFill };
                    }
                }
                catch { /* use default color */ }
            }

            Grid.SetColumn(avatar_border, 0);
            entry.Children.Add(avatar_border);

            // Action / file
            var right = new StackPanel { Spacing = 2 };

            if (!string.IsNullOrWhiteSpace(user_name))
            {
                right.Children.Add(new TextBlock
                {
                    Text       = user_name,
                    FontWeight = FontWeight.Bold,
                    FontSize   = 13,
                    TextTrimming = TextTrimming.CharacterEllipsis
                });
            }

            // Time
            int dd_pos = 0;
            while (true)
            {
                int dd_start = entry_block.IndexOf("<dd", dd_pos, StringComparison.OrdinalIgnoreCase);
                if (dd_start < 0) break;
                int dd_end = entry_block.IndexOf("</dd>", dd_start, StringComparison.OrdinalIgnoreCase);
                if (dd_end < 0) break;

                string dd = entry_block.Substring(dd_start, dd_end - dd_start + 5);
                string dd_text = StripTags(dd).Trim();
                string css_class = ExtractAttribute(dd, "class");

                if (!string.IsNullOrWhiteSpace(dd_text))
                {
                    // Remove the ? symbol (&#x25BE;) that comes from the HTML time link
                    dd_text = dd_text.Replace("\u25BE", "").Trim();

                    // Pick icon based on change type using Unicode escape sequences
                    // \u2795 = ?  \u2796 = ?  \u270F = ?  \u27A1 = ?  \u2022 = •
                    string icon = css_class switch
                    {
                        "added"   => "\u2795",  // ?
                        "deleted" => "\u2796",  // ?
                        "edited"  => "\u270F",  // ?
                        "moved"   => "\u27A1",  // ?
                        _         => "\u2022"   // •
                    };

                    var file_row = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 4 };
                    file_row.Children.Add(new TextBlock
                    {
                        Text    = icon,
                        Opacity = 0.6,
                        Width   = 16
                    });
                    file_row.Children.Add(new TextBlock
                    {
                        Text         = dd_text,
                        FontSize     = 12,
                        Opacity      = 0.75,
                        TextTrimming = TextTrimming.CharacterEllipsis
                    });
                    right.Children.Add(file_row);
                }
                dd_pos = dd_end + 5;
            }

            Grid.SetColumn(right, 1);
            entry.Children.Add(right);

            entry_border.Child = entry;
            _entriesPanel.Children.Add(entry_border);
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

        // Extract content of first <div class='cssClass'>...</div>
        private static string ExtractDivContent(string html, string css_class)
        {
            string search = $"class='{css_class}'";
            int class_pos = html.IndexOf(search, StringComparison.OrdinalIgnoreCase);
            if (class_pos < 0) return "";
            int content_start = html.IndexOf('>', class_pos) + 1;
            int content_end   = html.IndexOf("</div>", content_start, StringComparison.OrdinalIgnoreCase);
            if (content_start <= 0 || content_end < 0) return "";
            return html.Substring(content_start, content_end - content_start);
        }

        // Extract content of <dl>...</dl>
        private static string ExtractDlContent(string html)
        {
            int start = html.IndexOf("<dl", StringComparison.OrdinalIgnoreCase);
            if (start < 0) return "";
            int end = html.IndexOf("</dl>", start, StringComparison.OrdinalIgnoreCase);
            if (end < 0) return "";
            return html.Substring(start, end - start + 5);
        }

        // Extract avatar URL from style='background-image: url("...")'
        private static string ExtractAvatarUrl(string html)
        {
            string search = "background-image: url(\"";
            int start = html.IndexOf(search, StringComparison.OrdinalIgnoreCase);
            if (start < 0) return "";
            int url_start = start + search.Length;
            int url_end   = html.IndexOf("\")", url_start, StringComparison.OrdinalIgnoreCase);
            if (url_end < 0) return "";
            return html.Substring(url_start, url_end - url_start);
        }

        // Extract attribute value from an HTML tag
        private static string ExtractAttribute(string html, string attribute)
        {
            // Try class='...' and class="..."
            foreach (char q in new[] { '\'', '"' })
            {
                string search = $"{attribute}={q}";
                int start = html.IndexOf(search, StringComparison.OrdinalIgnoreCase);
                if (start < 0) continue;
                int val_start = start + search.Length;
                int val_end   = html.IndexOf(q, val_start);
                if (val_end < 0) continue;
                return html.Substring(val_start, val_end - val_start);
            }
            return "";
        }

        private static string StripTags(string html)
        {
            if (string.IsNullOrEmpty(html)) return "";
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

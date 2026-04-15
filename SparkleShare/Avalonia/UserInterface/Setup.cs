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
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Sparkles;

namespace SparkleShare.UserInterface
{
    public class Setup : Window
    {
        public SetupController Controller = new SetupController();

        // Content area replaced on each page change
        private Panel     _contentPanel  = null!;
        private Panel     _buttonPanel   = null!;
        private TextBlock _header        = null!;
        private TextBlock _description   = null!;

        public Setup()
        {
            Title  = "SparkleShare Setup";
            Width  = 640;
            Height = 440;
            CanResize = false;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            var icon = UserInterfaceHelpers.GetImageSource("sparkleshare-app", "ico");
            if (icon != null) Icon = new WindowIcon(icon);

            BuildShell();
            BindController();
        }

        // ?? Shell layout (side-splash + header + content + button bar) ????????
        private void BuildShell()
        {
            var root = new Grid();
            root.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(150)));
            root.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
            root.RowDefinitions.Add(new RowDefinition(new GridLength(1, GridUnitType.Star)));
            root.RowDefinitions.Add(new RowDefinition(new GridLength(58)));

            // Side splash
            var splash_bmp = UserInterfaceHelpers.GetImageSource("side-splash");
            if (splash_bmp != null)
            {
                var splash = new Image
                {
                    Source  = splash_bmp,
                    Width   = 150,
                    Stretch = Stretch.UniformToFill,
                    VerticalAlignment = VerticalAlignment.Stretch
                };
                Grid.SetColumn(splash, 0);
                Grid.SetRowSpan(splash, 2);
                root.Children.Add(splash);
            }

            // Right panel: header + description + content
            var right = new DockPanel { Margin = new Thickness(20, 16, 16, 0) };

            _header = new TextBlock
            {
                FontSize   = 16,
                Foreground = new SolidColorBrush(Color.FromRgb(0, 51, 153)),
                TextWrapping = TextWrapping.Wrap
            };
            DockPanel.SetDock(_header, Dock.Top);
            right.Children.Add(_header);

            _description = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                Margin       = new Thickness(0, 6, 0, 12),
                Opacity      = 0.8
            };
            DockPanel.SetDock(_description, Dock.Top);
            right.Children.Add(_description);

            _contentPanel = new Canvas { HorizontalAlignment = HorizontalAlignment.Stretch };
            right.Children.Add(_contentPanel);

            Grid.SetColumn(right, 1);
            Grid.SetRow(right, 0);
            root.Children.Add(right);

            // Bottom bar
            var bar = new Border
            {
                Background      = new SolidColorBrush(Color.FromRgb(240, 240, 240)),
                BorderBrush     = new SolidColorBrush(Color.FromRgb(223, 223, 223)),
                BorderThickness = new Thickness(0, 1, 0, 0)
            };
            _buttonPanel = new StackPanel
            {
                Orientation         = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment   = VerticalAlignment.Center,
                Margin              = new Thickness(0, 0, 8, 0),
                Spacing             = 6
            };
            bar.Child = _buttonPanel;
            Grid.SetColumn(bar, 0);
            Grid.SetColumnSpan(bar, 2);
            Grid.SetRow(bar, 1);
            root.Children.Add(bar);

            Content = root;
        }

        // ?? Controller binding ????????????????????????????????????????????????
        private void BindController()
        {
            Controller.ShowWindowEvent += () => Dispatcher.UIThread.Post(() => { Show(); Activate(); });
            Controller.HideWindowEvent += () => Dispatcher.UIThread.Post(Hide);
            Controller.ChangePageEvent += (type, warnings) =>
                Dispatcher.UIThread.Post(() => ShowPage(type, warnings));
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            e.Cancel = true;   // never close — hide instead
        }

        // ?? Page dispatcher ???????????????????????????????????????????????????
        private void ShowPage(PageType type, string[] warnings)
        {
            _contentPanel.Children.Clear();
            _buttonPanel.Children.Clear();

            switch (type)
            {
                case PageType.Setup:         ShowSetupPage();             break;
                case PageType.Invite:        ShowInvitePage();            break;
                case PageType.Add:           ShowAddPage();               break;
                case PageType.Syncing:       ShowSyncingPage();           break;
                case PageType.Error:         ShowErrorPage(warnings);     break;
                case PageType.StorageSetup:  ShowStoragePage();           break;
                case PageType.CryptoSetup:   ShowCryptoPage(false);       break;
                case PageType.CryptoPassword:ShowCryptoPage(true);        break;
                case PageType.Finished:      ShowFinishedPage(warnings);  break;
            }
        }

        // ?? Page: Setup ???????????????????????????????????????????????????????
        private void ShowSetupPage()
        {
            _header.Text      = "Welcome to SparkleShare!";
            _description.Text = "First off, what's your name and email?\n(Visible only to team members)";

            string name = Environment.UserName;

            var name_label  = Label("Full Name:", bold: true);
            var name_box    = new TextBox { Text = name, Width = 175 };
            var email_label = Label("Email:", bold: true);
            var email_box   = new TextBox { Width = 175 };

            var form = FormGrid(
                name_label,  name_box,
                email_label, email_box
            );
            _contentPanel.Children.Add(form);

            var cancel_btn   = Btn("Cancel");
            var continue_btn = Btn("Continue", isDefault: true);
            continue_btn.IsEnabled = false;

            AddButtons(cancel_btn, continue_btn);

            Controller.UpdateSetupContinueButtonEvent += enabled =>
                Dispatcher.UIThread.Post(() => continue_btn.IsEnabled = enabled);

            name_box.TextChanged  += (_, _) => Controller.CheckSetupPage(name_box.Text ?? "", email_box.Text ?? "");
            email_box.TextChanged += (_, _) => Controller.CheckSetupPage(name_box.Text ?? "", email_box.Text ?? "");
            cancel_btn.Click   += (_, _) => { SparkleShare.UI.StatusIcon.Dispose(); Controller.SetupPageCancelled(); };
            continue_btn.Click += (_, _) => Controller.SetupPageCompleted(name_box.Text ?? "", email_box.Text ?? "");

            Controller.CheckSetupPage(name_box.Text ?? "", email_box.Text ?? "");
            name_box.Focus();
        }

        // ?? Page: Invite ??????????????????????????????????????????????????????
        private void ShowInvitePage()
        {
            _header.Text      = "You've received an invite!";
            _description.Text = "Do you want to add this project to SparkleShare?";

            var form = FormGrid(
                Label("Address:"),     ReadOnly(Controller.PendingInvite!.Address),
                Label("Remote Path:"), ReadOnly(Controller.PendingInvite.RemotePath)
            );
            _contentPanel.Children.Add(form);

            var cancel_btn = Btn("Cancel");
            var add_btn    = Btn("Add", isDefault: true);
            AddButtons(cancel_btn, add_btn);

            cancel_btn.Click += (_, _) => Controller.PageCancelled();
            add_btn.Click    += (_, _) => Controller.InvitePageCompleted();
        }

        // ?? Page: Add ?????????????????????????????????????????????????????????
        private void ShowAddPage()
        {
            _header.Text      = "Where's your project hosted?";
            _description.Text = "";

            // Preset list
            var list = new ListBox
            {
                Width  = 419,
                Height = 175,
                SelectionMode = SelectionMode.Single
            };

            foreach (var preset in Controller.Presets)
            {
                var item_panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
                var preset_bmp = UserInterfaceHelpers.GetImage(preset.ImagePath);
                if (preset_bmp != null)
                    item_panel.Children.Add(new Image { Source = preset_bmp, Width = 24, Height = 24 });

                var text_stack = new StackPanel();
                text_stack.Children.Add(new TextBlock { Text = preset.Name, FontWeight = FontWeight.Bold });
                text_stack.Children.Add(new TextBlock { Text = preset.Description, Opacity = 0.6 });
                item_panel.Children.Add(text_stack);
                list.Items.Add(item_panel);
            }
            list.SelectedIndex = Controller.SelectedPresetIndex;

            var address_label = new TextBlock { Text = "Address:",     FontWeight = FontWeight.Bold };
            var address_box   = new TextBox   { Width = 200, Text = Controller.PreviousAddress,
                                                IsEnabled = Controller.SelectedPreset.Address == null };
            var address_help  = HelpLabel(Controller.SelectedPreset.AddressExample);

            var path_label    = new TextBlock { Text = "Remote Path:", FontWeight = FontWeight.Bold };
            var path_box      = new TextBox   { Width = 200, Text = Controller.PreviousPath,
                                                IsEnabled = Controller.SelectedPreset.Path == null };
            var path_help     = HelpLabel(Controller.SelectedPreset.PathExample);

            var history_cb = new CheckBox { Content = "Fetch prior revisions", IsChecked = Controller.FetchPriorHistory };

            // Layout
            var outer = new StackPanel { Spacing = 6 };
            outer.Children.Add(list);

            var addr_col  = Col(address_label, address_box, address_help);
            var path_col  = Col(path_label,    path_box,    path_help);
            var fields    = new Grid();
            fields.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
            fields.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
            Grid.SetColumn(addr_col, 0);
            Grid.SetColumn(path_col, 1);
            fields.Children.Add(addr_col);
            fields.Children.Add(path_col);
            outer.Children.Add(fields);
            outer.Children.Add(history_cb);
            _contentPanel.Children.Add(outer);

            var cancel_btn = Btn("Cancel");
            var add_btn    = Btn("Add", isDefault: true);
            add_btn.IsEnabled = false;
            AddButtons(cancel_btn, add_btn);

            // Events
            Controller.ChangeAddressFieldEvent += (text, example, state) => Dispatcher.UIThread.Post(() =>
            {
                address_box.Text      = text;
                address_box.IsEnabled = state == FieldState.Enabled;
                address_help.Text     = example;
            });
            Controller.ChangePathFieldEvent += (text, example, state) => Dispatcher.UIThread.Post(() =>
            {
                path_box.Text      = text;
                path_box.IsEnabled = state == FieldState.Enabled;
                path_help.Text     = example;
            });
            Controller.UpdateAddProjectButtonEvent += enabled =>
                Dispatcher.UIThread.Post(() => add_btn.IsEnabled = enabled);

            list.SelectionChanged   += (_, _) => Controller.SelectedPresetChanged(list.SelectedIndex);
            address_box.TextChanged += (_, _) => Controller.CheckAddPage(address_box.Text ?? "", path_box.Text ?? "", list.SelectedIndex);
            path_box.TextChanged    += (_, _) => Controller.CheckAddPage(address_box.Text ?? "", path_box.Text ?? "", list.SelectedIndex);
            history_cb.IsCheckedChanged += (_, _) => Controller.HistoryItemChanged(history_cb.IsChecked == true);
            cancel_btn.Click += (_, _) => Controller.PageCancelled();
            add_btn.Click    += (_, _) => Controller.AddPageCompleted(address_box.Text ?? "", path_box.Text ?? "");

            Controller.CheckAddPage(address_box.Text ?? "", path_box.Text ?? "", list.SelectedIndex);
            address_box.Focus();
        }

        // ?? Page: Syncing ?????????????????????????????????????????????????????
        private void ShowSyncingPage()
        {
            _header.Text      = $"Adding project '{Controller.SyncingFolder}'…";
            _description.Text = "This may take a while for large projects.\nIsn't it coffee-o'clock?";

            var progress = new ProgressBar { Width = 414, Height = 15, Value = Controller.ProgressBarPercentage };
            var info_lbl = new TextBlock   { Width = 414, Text = "Preparing to fetch files…", TextAlignment = TextAlignment.Right };

            var stack = new StackPanel { Spacing = 8 };
            stack.Children.Add(progress);
            stack.Children.Add(info_lbl);
            _contentPanel.Children.Add(stack);

            var cancel_btn  = Btn("Cancel");
            var finish_btn  = Btn("Finish", isDefault: true);
            finish_btn.IsEnabled = false;
            AddButtons(cancel_btn, finish_btn);

            Controller.UpdateProgressBarEvent += (pct, speed) => Dispatcher.UIThread.Post(() =>
            {
                progress.Value = pct;
                info_lbl.Text  = speed;
            });
            cancel_btn.Click += (_, _) => Controller.SyncingCancelled();
        }

        // ?? Page: Error ???????????????????????????????????????????????????????
        private void ShowErrorPage(string[] warnings)
        {
            _header.Text      = "Oops! Something went wrong…";
            _description.Text = "Please check the following:";

            var stack = new StackPanel { Spacing = 6 };

            stack.Children.Add(new TextBlock
            {
                Text = $"• {Controller.PreviousUrl} is the address we've compiled. Does this look alright?\n" +
                        "• Is this computer's Client ID known by the host?",
                TextWrapping = TextWrapping.Wrap
            });

            if (warnings?.Length > 0)
            {
                stack.Children.Add(new TextBlock { Text = "• Raw error:", FontWeight = FontWeight.Bold });
                foreach (var w in warnings)
                    stack.Children.Add(new TextBlock { Text = "  " + w, TextWrapping = TextWrapping.Wrap, FontWeight = FontWeight.Bold });
            }

            _contentPanel.Children.Add(stack);

            var cancel_btn    = Btn("Cancel");
            var try_again_btn = Btn("Try again…", isDefault: true);
            AddButtons(cancel_btn, try_again_btn);

            cancel_btn.Click    += (_, _) => Controller.PageCancelled();
            try_again_btn.Click += (_, _) => Controller.ErrorPageCompleted();
        }

        // ?? Page: StorageSetup ????????????????????????????????????????????????
        private void ShowStoragePage()
        {
            _header.Text      = $"Storage type for '{Controller.SyncingFolder}'";
            _description.Text = "What type of storage would you like to use?";

            var stack  = new StackPanel { Spacing = 12 };
            var radios = new List<RadioButton>();

            foreach (var storage in SparkleShare.Controller.FetcherAvailableStorageTypes)
            {
                var text = new StackPanel();
                text.Children.Add(new TextBlock { Text = storage.Name, FontWeight = FontWeight.Bold });
                text.Children.Add(new TextBlock { Text = storage.Description, Opacity = 0.7 });

                var rb = new RadioButton { Content = text };
                radios.Add(rb);
                stack.Children.Add(rb);
            }

            if (radios.Count > 0) radios[0].IsChecked = true;
            _contentPanel.Children.Add(stack);

            var cancel_btn   = Btn("Cancel");
            var continue_btn = Btn("Continue", isDefault: true);
            AddButtons(cancel_btn, continue_btn);

            cancel_btn.Click += (_, _) => Controller.SyncingCancelled();
            continue_btn.Click += (_, _) =>
            {
                for (int i = 0; i < radios.Count; i++)
                {
                    if (radios[i].IsChecked == true)
                    {
                        Controller.StoragePageCompleted(SparkleShare.Controller.FetcherAvailableStorageTypes[i].Type);
                        return;
                    }
                }
            };
        }

        // ?? Page: CryptoSetup / CryptoPassword ????????????????????????????????
        private void ShowCryptoPage(bool is_password_page)
        {
            _header.Text      = is_password_page ? "This project contains encrypted files"
                                                  : "Set up file encryption";
            _description.Text = is_password_page ? "Please enter the password to see their contents."
                                                  : "Please provide a strong password that you don't use elsewhere.";

            var pw_label   = new TextBlock { Text = "Password:", FontWeight = FontWeight.Bold };
            var pw_box     = new TextBox   { Width = 200, PasswordChar = '?' };
            var show_cb    = new CheckBox  { Content = "Show password" };

            var info = is_password_page ? null : new TextBlock
            {
                Text         = "This password can't be changed later, and your files can't be recovered if it's forgotten.",
                TextWrapping = TextWrapping.Wrap,
                Width        = 315,
                Opacity      = 0.7
            };

            var stack = new StackPanel { Spacing = 8 };
            stack.Children.Add(FormGrid(pw_label, pw_box));
            stack.Children.Add(show_cb);
            if (info != null) stack.Children.Add(info);
            _contentPanel.Children.Add(stack);

            var cancel_btn   = Btn("Cancel");
            var continue_btn = Btn("Continue", isDefault: true);
            continue_btn.IsEnabled = false;
            AddButtons(cancel_btn, continue_btn);

            show_cb.IsCheckedChanged += (_, _) =>
                pw_box.PasswordChar = show_cb.IsChecked == true ? '\0' : '?';

            pw_box.TextChanged += (_, _) =>
            {
                if (is_password_page) Controller.CheckCryptoPasswordPage(pw_box.Text ?? "");
                else                  Controller.CheckCryptoSetupPage(pw_box.Text ?? "");
            };

            if (is_password_page)
                Controller.UpdateCryptoPasswordContinueButtonEvent += e => Dispatcher.UIThread.Post(() => continue_btn.IsEnabled = e);
            else
                Controller.UpdateCryptoSetupContinueButtonEvent    += e => Dispatcher.UIThread.Post(() => continue_btn.IsEnabled = e);

            cancel_btn.Click   += (_, _) => Controller.CryptoPageCancelled();
            continue_btn.Click += (_, _) =>
            {
                if (is_password_page) Controller.CryptoPasswordPageCompleted(pw_box.Text ?? "");
                else                  Controller.CryptoSetupPageCompleted(pw_box.Text ?? "");
            };

            pw_box.Focus();
        }

        // ?? Page: Finished ????????????????????????????????????????????????????
        private void ShowFinishedPage(string[] warnings)
        {
            _header.Text      = "Your shared project is ready!";
            _description.Text = "You can find the files in your SparkleShare folder.";

            if (warnings?.Length > 0)
            {
                var stack = new StackPanel { Spacing = 4 };
                foreach (var w in warnings)
                    stack.Children.Add(new TextBlock { Text = "? " + w, TextWrapping = TextWrapping.Wrap });
                _contentPanel.Children.Add(stack);
            }

            var show_files_btn = Btn("Show files");
            var finish_btn     = Btn("Finish", isDefault: true);
            AddButtons(show_files_btn, finish_btn);

            show_files_btn.Click += (_, _) => Controller.ShowFilesClicked();
            finish_btn.Click     += (_, _) => Controller.FinishPageCompleted();
        }

        // ?? Helpers ???????????????????????????????????????????????????????????
        private static TextBlock Label(string text, bool bold = false) =>
            new TextBlock { Text = text, FontWeight = bold ? FontWeight.Bold : FontWeight.Normal,
                            VerticalAlignment = VerticalAlignment.Center };

        private static TextBlock ReadOnly(string text) =>
            new TextBlock { Text = text, FontWeight = FontWeight.Bold,
                            VerticalAlignment = VerticalAlignment.Center };

        private static TextBlock HelpLabel(string text) =>
            new TextBlock { Text = text ?? "", FontSize = 11, Opacity = 0.6 };

        private static StackPanel Col(params Control[] controls)
        {
            var sp = new StackPanel { Spacing = 4, Margin = new Thickness(0, 0, 8, 0) };
            foreach (var c in controls) sp.Children.Add(c);
            return sp;
        }

        private static Grid FormGrid(params Control[] pairs)
        {
            var grid = new Grid { ColumnDefinitions = { new(GridLength.Auto), new(new GridLength(1, GridUnitType.Star)) } };
            for (int i = 0; i < pairs.Length; i += 2)
            {
                grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                int row = i / 2;
                Grid.SetRow(pairs[i],     row); Grid.SetColumn(pairs[i],     0);
                Grid.SetRow(pairs[i + 1], row); Grid.SetColumn(pairs[i + 1], 1);
                pairs[i].Margin     = new Thickness(0, 4, 10, 4);
                pairs[i + 1].Margin = new Thickness(0, 4, 0,  4);
                grid.Children.Add(pairs[i]);
                grid.Children.Add(pairs[i + 1]);
            }
            return grid;
        }

        private static Button Btn(string label, bool isDefault = false) =>
            new Button { Content = label, MinWidth = 75, Margin = new Thickness(0, 6, 0, 6) };

        private void AddButtons(params Button[] buttons)
        {
            foreach (var b in buttons) _buttonPanel.Children.Add(b);
        }
    }
}

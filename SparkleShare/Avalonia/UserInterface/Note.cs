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

using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace SparkleShare.UserInterface
{
    public class Note : Window
    {
        public NoteController Controller = new NoteController();

        private TextBox   _noteBox   = null!;
        private Button    _syncButton = null!;
        private TextBlock _titleLabel = null!;

        public Note()
        {
            Title  = "Add Note";
            Width  = 400;
            Height = 230;
            CanResize = false;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            var icon = UserInterfaceHelpers.GetImageSource("sparkleshare-app", "ico");
            if (icon != null) Icon = new WindowIcon(icon);

            BuildUI();
            BindController();
        }

        private void BuildUI()
        {
            var grid = new Grid { Margin = new Thickness(16) };
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            grid.RowDefinitions.Add(new RowDefinition(new GridLength(1, GridUnitType.Star)));
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

            // Avatar + title row
            var header = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };

            var avatar_path = Controller.AvatarFilePath;
            if (!string.IsNullOrEmpty(avatar_path))
            {
                var avatar_bitmap = UserInterfaceHelpers.GetImage(avatar_path);
                if (avatar_bitmap != null)
                {
                    var avatar = new Image { Width = 48, Height = 48, Source = avatar_bitmap };
                    header.Children.Add(avatar);
                }
            }

            _titleLabel = new TextBlock
            {
                Text       = "Sync and add a note",
                FontWeight = FontWeight.Bold,
                FontSize   = 14,
                VerticalAlignment = VerticalAlignment.Center
            };
            header.Children.Add(_titleLabel);

            Grid.SetRow(header, 0);
            grid.Children.Add(header);

            // Description
            var desc = new TextBlock
            {
                Text   = "This note will be visible to everyone when viewing recent changes.",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 8, 0, 8),
                Opacity = 0.7
            };
            Grid.SetRow(desc, 1);
            grid.Children.Add(desc);

            // Note input
            _noteBox = new TextBox
            {
                Watermark    = "Type your note here…",
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                Margin       = new Thickness(0, 0, 0, 8)
            };
            Grid.SetRow(_noteBox, 2);
            grid.Children.Add(_noteBox);

            // Buttons
            var cancel_button = new Button { Content = "Cancel",          HorizontalAlignment = HorizontalAlignment.Left };
            _syncButton        = new Button { Content = "Sync and Finish", HorizontalAlignment = HorizontalAlignment.Right };

            var button_row = new Grid();
            button_row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            button_row.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
            button_row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

            Grid.SetColumn(cancel_button, 0);
            Grid.SetColumn(_syncButton,   2);
            button_row.Children.Add(cancel_button);
            button_row.Children.Add(_syncButton);

            Grid.SetRow(button_row, 3);
            grid.Children.Add(button_row);

            Content = grid;

            cancel_button.Click += (_, _) => Controller.CancelClicked();
            _syncButton.Click   += (_, _) => Controller.SyncClicked(_noteBox.Text ?? "");
        }

        private void BindController()
        {
            Controller.ShowWindowEvent += () => Dispatcher.UIThread.Post(() =>
            {
                _noteBox.Text = "";
                Show();
                Activate();
                _noteBox.Focus();
            });

            Controller.HideWindowEvent  += () => Dispatcher.UIThread.Post(Hide);
            Controller.UpdateTitleEvent += title =>
                Dispatcher.UIThread.Post(() => _titleLabel.Text = "Note for '" + title + "'");
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            e.Cancel = true;
            Controller.WindowClosed();
        }
    }
}

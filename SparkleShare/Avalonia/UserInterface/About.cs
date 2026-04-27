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
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace SparkleShare.UserInterface
{
    public class About : Window
    {
        public AboutController Controller = new AboutController();

        private TextBlock _updatesLabel = null!;

        public About()
        {
            Title  = "About SparkleShare";
            Width  = 720;
            Height = 288;
            CanResize = false;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            var icon = UserInterfaceHelpers.GetImageSource("sparkleshare-app", "ico");
            if (icon != null) Icon = new WindowIcon(icon);

            BuildUI();
            BindController();
        }

        private void BuildUI()
        {
            // Background image
            var canvas = new Canvas { Width = 720, Height = 288 };

            var about_image = UserInterfaceHelpers.GetImageSource("about");
            if (about_image != null)
            {
                var img = new Image { Width = 720, Height = 260, Source = about_image };
                canvas.Children.Add(img);
                Canvas.SetLeft(img, 0);
                Canvas.SetTop(img, 0);
            }

            // Version label
            var version = new TextBlock
            {
                Text       = "version " + Controller.RunningVersion,
                FontSize   = 11,
                Foreground = Brushes.White
            };
            canvas.Children.Add(version);
            Canvas.SetLeft(version, 294);
            Canvas.SetTop(version, 92);

            // Updates label
            _updatesLabel = new TextBlock
            {
                Text       = "Checking for updates…",
                FontSize   = 11,
                Foreground = new SolidColorBrush(Color.FromArgb(128, 255, 255, 255))
            };
            canvas.Children.Add(_updatesLabel);
            Canvas.SetLeft(_updatesLabel, 294);
            Canvas.SetTop(_updatesLabel, 109);

            // Credits text
            var credits = new TextBlock
            {
                FontSize     = 11,
                Foreground   = Brushes.White,
                Text         = "Copyright © 2010–" + DateTime.Now.Year + " Hylke Bons and others.\n\n" +
                               "SparkleShare is Open Source software. You are free to use, modify, " +
                               "and redistribute it under the GNU General Public License version 3 or later.",
                TextWrapping = TextWrapping.Wrap,
                Width        = 318
            };
            canvas.Children.Add(credits);
            Canvas.SetLeft(credits, 294);
            Canvas.SetTop(credits, 142);

            // Links
            double link_top = 222;
            double link_left = 294;

            foreach (var (title, url) in new (string, string)[]
            {
                ("Website",          Controller.WebsiteLinkAddress),
                ("Credits",          Controller.CreditsLinkAddress),
                ("Report a problem", Controller.ReportProblemLinkAddress),
                ("Debug log",        Controller.DebugLogLinkAddress),
            })
            {
                var link = CreateLink(title, url);
                canvas.Children.Add(link);
                Canvas.SetLeft(link, link_left);
                Canvas.SetTop(link, link_top);
                link.Measure(Size.Infinity);
                link_left += link.DesiredSize.Width + 20;
            }

            Content = canvas;
        }

        private TextBlock CreateLink(string title, string url)
        {
            var tb = new TextBlock
            {
                Text       = title,
                FontSize   = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(135, 178, 227)),
                Cursor     = new Cursor(StandardCursorType.Hand),
                TextDecorations = TextDecorations.Underline
            };

            tb.PointerPressed += (_, _) => SparkleShare.Controller.OpenWebsite(url);
            return tb;
        }

        private void BindController()
        {
            Controller.ShowWindowEvent += () => Dispatcher.UIThread.Post(() => { Show(); Activate(); });
            Controller.HideWindowEvent += () => Dispatcher.UIThread.Post(Hide);

            Controller.UpdateLabelEvent += text =>
                Dispatcher.UIThread.Post(() => _updatesLabel.Text = text);
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            e.Cancel = true;
            Controller.WindowClosed();
        }
    }
}

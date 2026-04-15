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
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using SparkleShare.UserInterface;

namespace SparkleShare
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Critical: set to OnExplicitShutdown so app doesn't exit when no windows are visible
                desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnExplicitShutdown;

                // Create UI on the UI thread
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    try
                    {
                        var ui = new UserInterface.UserInterface();
                        SparkleShare.UI = ui;
                        Sparkles.Logger.LogInfo("App", "UserInterface initialized successfully");
                    }
                    catch (Exception ex)
                    {
                        Sparkles.Logger.LogInfo("App", "Failed to initialize UI", ex);
                        desktop.Shutdown();
                    }
                }, Avalonia.Threading.DispatcherPriority.Normal);

                AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                {
                    if (e.ExceptionObject is Exception ex)
                        Sparkles.Logger.WriteCrashReport(ex);
                    Environment.Exit(-1);
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}

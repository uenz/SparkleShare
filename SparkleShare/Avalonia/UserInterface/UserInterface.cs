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
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Sparkles;

namespace SparkleShare.UserInterface
{
    public class UserInterface : IUserInterface
    {
        public Setup? Setup;
        public EventLog? EventLog;
        public Bubbles? Bubbles;
        public StatusIcon? StatusIcon;
        public About? About;
        public Note? Note;

        IStatusIcon IUserInterface.StatusIcon => StatusIcon!;
        IBubbles IUserInterface.Bubbles => Bubbles!;
        IEventLog IUserInterface.EventLog => EventLog!;

        public UserInterface()
        {
            try
            {
                Logger.LogInfo("UserInterface", "Creating UI components...");
                
                Setup      = new Setup();
                EventLog   = new EventLog();
                About      = new About();
                Bubbles    = new Bubbles();
                StatusIcon = new StatusIcon();
                Note       = new Note();

                Logger.LogInfo("UserInterface", "Calling Controller.UIHasLoaded()...");
                SparkleShare.Controller.UIHasLoaded();
                Logger.LogInfo("UserInterface", "UI initialization complete");
            }
            catch (Exception ex)
            {
                Logger.LogInfo("UserInterface", "Failed to initialize UserInterface", ex);
                throw;
            }
        }

        public void Run(string[] args) { }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs exception_args)
        {
            try
            {
                if (exception_args.ExceptionObject is Exception exception)
                {
                    Logger.WriteCrashReport(exception);
                }
            }
            finally
            {
                Environment.Exit(-1);
            }
        }

        private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            try
            {
                Logger.WriteCrashReport(e.Exception);
            }
            finally
            {
                Environment.Exit(-1);
            }
        }
    }
}

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
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Threading;

using Sparkles;

namespace SparkleShare {

    public class AboutController {

        public event Action ShowWindowEvent = delegate { };
        public event Action HideWindowEvent = delegate { };

        public event UpdateLabelEventDelegate UpdateLabelEvent = delegate { };
        public delegate void UpdateLabelEventDelegate (string text);
        // TODO: get link to issues from static configuration
        public readonly string WebsiteLinkAddress       = "https://github.com/uenz/SparkleShare/wiki";
        public readonly string CreditsLinkAddress       = "https://github.com/uenz/SparkleShare/blob/master/.github/AUTHORS.md";
        public readonly string ReportProblemLinkAddress = "https://www.github.com/uenz/SparkleShare/issues";
        public readonly string DebugLogLinkAddress      = "file://" + SparkleShare.Controller.Config.LogFilePath;

        public string RunningVersion;


        public AboutController ()
        {
            RunningVersion = InstallationInfo.Version;

            SparkleShare.Controller.ShowAboutWindowEvent += delegate {
                ShowWindowEvent ();
                new Thread (CheckForNewVersion).Start ();
            };
        }


        public void WindowClosed ()
        {
            HideWindowEvent ();
        }


        void CheckForNewVersion ()
        {
            UpdateLabelEvent ("Checking for updates…");
            Thread.Sleep (500);

            ServicePointManager.SecurityProtocol = SecurityProtocolType.SystemDefault;

            var uri = new Uri ("https://raw.githubusercontent.com/uenz/SparkleShare/refs/heads/master/version-latest");
            HttpClient client = new();

            try
            {
                string latest_version = client.GetStringAsync(uri).GetAwaiter().GetResult().Split(' ')[0].Trim();

                if (new Version(latest_version) > new Version(RunningVersion))
                    UpdateLabelEvent("An update (version " + latest_version + ") is available!");
                else
                    UpdateLabelEvent("✓ You are running the latest version");
            }
            catch (Exception e)
            {
                Logger.LogInfo("UI", "Failed to download " + uri, e);
                UpdateLabelEvent("Couldn’t check for updates\t");
            }
        }
    }
}

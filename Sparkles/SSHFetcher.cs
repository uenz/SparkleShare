//   SparkleShare, a collaboration and sharing tool.
//   Copyright (C) 2010  Hylke Bons <hi@planetpeanut.uk>
//
//   This program is free software: you can redistribute it and/or modify
//   it under the terms of the GNU Lesser General Public License as
//   published by the Free Software Foundation, either version 3 of the
//   License, or (at your option) any later version.
//
//   This program is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//   GNU General Public License for more details.
//
//   You should have received a copy of the GNU General Public License
//   along with this program. If not, see <http://www.gnu.org/licenses/>.


using System;
using System.IO;
using System.Security.Cryptography;

namespace Sparkles
{

    public abstract class SSHFetcher : BaseFetcher
    {

        protected SSHFetcher(SparkleFetcherInfo info) : base(info)
        {
        }


        public override bool Fetch()
        {
            string? host_key = FetchHostKey();
            bool host_key_warning = false;

            if (string.IsNullOrEmpty(RemoteUrl.Host) || host_key == null)
            {
                Logger.LogInfo("Auth", "Could not fetch host key");
                errors.Add("error: Could not fetch host key");

                return false;
            }

            if (RequiredFingerprint != null)
            {
                string? host_fingerprint = DeriveFingerprint(host_key);

                if (host_fingerprint == null || RequiredFingerprint != host_fingerprint)
                {
                    Logger.LogInfo("Auth", "Fingerprint doesn't match");
                    errors.Add("error: Host fingerprint doesn't match");

                    return false;
                }

                Logger.LogInfo("Auth", "Fingerprint matches");

            }
            else
            {
                Logger.LogInfo("Auth", "Skipping fingerprint check");
                host_key_warning = true;
            }

            AcceptHostKey(host_key, host_key_warning);
            return true;
        }


        string? FetchHostKey()
        {
            Logger.LogInfo("Auth", string.Format("Fetching host key for {0}", RemoteUrl.Host));
            SSHCommand? ssh_keyscan = new SSHCommand(SSHCommand.SSHKeyScanCommandPath, string.Format("-t rsa -p 22 {0}", RemoteUrl.Host));

            if (RemoteUrl.Port > 0)
                ssh_keyscan.StartInfo.Arguments = string.Format("-t rsa -p {0} {1}", RemoteUrl.Port, RemoteUrl.Host);

            string host_key = ssh_keyscan.StartAndReadStandardOutput();
            //TODO Error handlin if no key is delivered
            if (ssh_keyscan.ExitCode == 0 && !string.IsNullOrWhiteSpace(host_key))
                return host_key;

            return null;
        }


        string? DeriveFingerprint(string public_key)
        {
            try
            {
                //SHA256 sha256 = new SHA256CryptoServiceProvider ();
                string key = public_key.Split(" ".ToCharArray())[2];

                string fingerprint = Convert.FromBase64String(key).SHA256();
                fingerprint = fingerprint.ToLower().Replace("-", ":");

                return fingerprint;

            }
            catch (Exception e)
            {
                Logger.LogInfo("Fetcher", "Failed to create fingerprint: ", e);
                return null;
            }
        }


        void AcceptHostKey(string host_key, bool warn)
        {
            string ssh_config_path = Path.Combine(Configuration.DefaultConfiguration.DirectoryPath, "ssh");
            string known_hosts_file_path = Path.Combine(ssh_config_path, "known_hosts");

            if (!File.Exists(known_hosts_file_path))
            {
                if (!Directory.Exists(ssh_config_path))
                    Directory.CreateDirectory(ssh_config_path);

                File.Create(known_hosts_file_path).Close();
            }

            string host = RemoteUrl.Host;
            string known_hosts = File.ReadAllText(known_hosts_file_path);
            string[] known_hosts_lines = File.ReadAllLines(known_hosts_file_path);

            foreach (string line in known_hosts_lines)
            {
                if (line.StartsWith(host + " ", StringComparison.InvariantCulture))
                    return;
            }

            if (known_hosts.EndsWith("\n", StringComparison.InvariantCulture))
                File.AppendAllText(known_hosts_file_path, host_key + "\n");
            else
                File.AppendAllText(known_hosts_file_path, "\n" + host_key + "\n");

            Logger.LogInfo("Auth", "Accepted host key for " + host);

            if (warn)
                warnings.Add("The following host key has been accepted:\n" + DeriveFingerprint(host_key));
        }
    }
}

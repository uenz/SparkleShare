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
            Logger.LogInfo("Auth", string.Format ("Fetching host key for {0}", RemoteUrl.Host));

            // Scan for all common key types so the correct one is stored in known_hosts,
            // regardless of which algorithm the server prefers (rsa, ecdsa, ed25519)
            string key_types = "ecdsa,ed25519,rsa";
            var ssh_keyscan = new SSHCommand (SSHCommand.SSHKeyScanCommandPath, string.Format ("-t {0} -p 22 {1}", key_types, RemoteUrl.Host));

            if (RemoteUrl.Port > 0)
                ssh_keyscan.StartInfo.Arguments = string.Format ("-t {0} -p {1} {2}", key_types, RemoteUrl.Port, RemoteUrl.Host);

            string host_key = ssh_keyscan.StartAndReadStandardOutput ();

            if (ssh_keyscan.ExitCode == 0 && !string.IsNullOrWhiteSpace (host_key))
                return host_key;

            Logger.LogInfo ("Auth", string.Format ("ssh-keyscan exited with code {0}", ssh_keyscan.ExitCode));
            return null;
        }


        string? DeriveFingerprint(string public_key)
        {
            try {
                SHA256 sha256 = System.Security.Cryptography.SHA256.Create();
                string key = public_key.Split(" ".ToCharArray()) [2];

                byte [] base64_bytes = Convert.FromBase64String (key);
                byte [] sha256_bytes = sha256.ComputeHash(base64_bytes);

                string fingerprint = BitConverter.ToString(sha256_bytes);
                fingerprint = fingerprint.ToLower().Replace("-", ":");

                return fingerprint;

            } catch (Exception e) {
                Logger.LogInfo ("Fetcher", "Failed to create fingerprint: ", e);
                return null;
            }
        }


		void AcceptHostKey(string host_key, bool warn)
        {
            string ssh_config_path = Path.Combine(Configuration.DefaultConfiguration.DirectoryPath, "ssh");
            string known_hosts_file_path = Path.Combine(ssh_config_path, "known_hosts");

            if (!File.Exists(known_hosts_file_path)) {
                if (!Directory.Exists(ssh_config_path))
                    Directory.CreateDirectory(ssh_config_path);

                File.Create (known_hosts_file_path).Close ();
            }

            string host                   = RemoteUrl.Host;
            string known_hosts_content    = File.ReadAllText(known_hosts_file_path);
            string [] known_hosts_lines   = File.ReadAllLines(known_hosts_file_path);

            // ssh-keyscan may return multiple lines (one per key type).
            // Add each line individually if not already present.
            bool any_added = false;

            foreach (string new_key_line in host_key.Split(new [] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)) {
                if (string.IsNullOrWhiteSpace(new_key_line) || new_key_line.StartsWith ("#"))
                    continue;

                // Check if an identical line already exists in known_hosts
                bool already_known = false;

                foreach (string existing_line in known_hosts_lines) {
                    if (existing_line.Equals (new_key_line, StringComparison.InvariantCulture)) {
                        already_known = true;
                        break;
                    }
                }

                if (!already_known) {
                    if (known_hosts_content.EndsWith("\n", StringComparison.InvariantCulture) || known_hosts_content.Length == 0)
                        File.AppendAllText(known_hosts_file_path, new_key_line + "\n");
                    else
                        File.AppendAllText(known_hosts_file_path, "\n" + new_key_line + "\n");

                    known_hosts_content += new_key_line + "\n";
                    Logger.LogInfo("Auth", "Accepted host key for " + host + ": " + new_key_line.Split (' ') [1]);
                    any_added = true;
                }
            }

            if (any_added && warn)
                warnings.Add("The following host key has been accepted:\n" + DeriveFingerprint (host_key));
        }
    }
}

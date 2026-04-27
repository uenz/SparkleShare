// Copyright (C) 2024 SparkleShare Contributors
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using Sparkles;

namespace Sparkles.Tests
{
    // ?? Concrete stub so we can instantiate the abstract SSHFetcher ???????????
    internal class StubSSHFetcher : SSHFetcher
    {
        // Injected host key returned by FetchHostKey via reflection helper
        public string? InjectedHostKey { get; set; }
        public bool FetchWasCalled      { get; private set; }
        public bool StopWasCalled       { get; private set; }

        public StubSSHFetcher(SparkleFetcherInfo info) : base(info) { }

        // ?? Abstract members ?????????????????????????????????????????????????
        public override void Stop()                              => StopWasCalled = true;
        protected override bool IsFetchedRepoEmpty               => false;
        public override bool IsFetchedRepoPasswordCorrect(string p) => true;
        public override void EnableFetchedRepoCrypto(string p)   { }

        // Override Fetch so we can inject a host key without a real SSH call
        public override bool Fetch()
        {
            FetchWasCalled = true;
            // Inject host key via the private field through the base call
            // We use the helper below when we need to test AcceptHostKey logic.
            return base.Fetch();
        }

        // ?? Test helpers ?????????????????????????????????????????????????????

        /// <summary>Calls the private DeriveFingerprint method via reflection.</summary>
        public string? CallDeriveFingerprint(string public_key)
        {
            var method = typeof(SSHFetcher).GetMethod(
                "DeriveFingerprint",
                BindingFlags.NonPublic | BindingFlags.Instance);

            return (string?)method!.Invoke(this, new object[] { public_key });
        }

        /// <summary>Calls the private AcceptHostKey method via reflection.</summary>
        public void CallAcceptHostKey(string host_key, bool warn)
        {
            var method = typeof(SSHFetcher).GetMethod(
                "AcceptHostKey",
                BindingFlags.NonPublic | BindingFlags.Instance);

            method!.Invoke(this, new object[] { host_key, warn });
        }
    }

    // ?? Helper to create a standard SparkleFetcherInfo ????????????????????????
    internal static class FetcherInfoFactory
    {
        public static SparkleFetcherInfo Create(
            string address     = "ssh://git@github.com",
            string remotePath  = "/user/repo.git",
            string fingerprint = "",
            string targetDir   = "")
        {
            return new SparkleFetcherInfo
            {
                Address          = address,
                RemotePath       = remotePath,
                Fingerprint      = fingerprint,
                Backend          = "Git",
                TargetDirectory  = string.IsNullOrEmpty(targetDir)
                                    ? Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
                                    : targetDir
            };
        }
    }


    // ?? DeriveFingerprint ?????????????????????????????????????????????????????
    [TestFixture]
    public class TestDeriveFingerprint
    {
        private StubSSHFetcher _fetcher = null!;

        [SetUp]
        public void SetUp()
        {
            _fetcher = new StubSSHFetcher(FetcherInfoFactory.Create());
        }

        [Test]
        public void ReturnsNullForNullInput()
        {
            // DeriveFingerprint splits on spaces and accesses [2] — null throws
            var result = _fetcher.CallDeriveFingerprint(null!);
            Assert.That(result, Is.Null);
        }

        [Test]
        public void ReturnsNullForTooFewParts()
        {
            var result = _fetcher.CallDeriveFingerprint("only-one-part");
            Assert.That(result, Is.Null);
        }

        [Test]
        public void ReturnsNullForInvalidBase64()
        {
            var result = _fetcher.CallDeriveFingerprint("ssh-rsa type NOT_VALID_BASE64!!!");
            Assert.That(result, Is.Null);
        }

        [Test]
        public void ReturnsFingerprintForValidKey()
        {
            // Construct a minimal but valid public-key-like string:
            // "<type> <ignored> <valid-base64>"
            string payload  = "SparkleShareTestKey";
            string b64      = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(payload));
            string key_line = $"ssh-rsa AAAA {b64}";

            var result = _fetcher.CallDeriveFingerprint(key_line);

            Assert.That(result, Is.Not.Null.And.Not.Empty);
            // Fingerprint must be lower-case hex separated by colons
            Assert.That(result, Does.Match(@"^[0-9a-f:]+$"));
        }

        [Test]
        public void FingerprintIsLowercaseAndColonSeparated()
        {
            string b64      = Convert.ToBase64String(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
            string key_line = $"ssh-rsa AAAA {b64}";

            var result = _fetcher.CallDeriveFingerprint(key_line);

            Assert.That(result, Is.Not.Null);
            // Result must be lower-case
            Assert.That(result, Is.EqualTo(result!.ToLower()));
            // Result must only contain hex chars and colons (after Replace("-", ":"))
            Assert.That(result, Does.Match(@"^[0-9a-f:]+$"));
        }

        [Test]
        public void SameFingerprintForSameKey()
        {
            string b64      = Convert.ToBase64String(new byte[] { 9, 8, 7, 6, 5 });
            string key_line = $"ssh-rsa AAAA {b64}";

            var r1 = _fetcher.CallDeriveFingerprint(key_line);
            var r2 = _fetcher.CallDeriveFingerprint(key_line);

            Assert.That(r1, Is.EqualTo(r2));
        }

        [Test]
        public void DifferentFingerprintsForDifferentKeys()
        {
            string b64a = Convert.ToBase64String(new byte[] { 1, 2, 3 });
            string b64b = Convert.ToBase64String(new byte[] { 4, 5, 6 });

            var r1 = _fetcher.CallDeriveFingerprint($"ssh-rsa X {b64a}");
            var r2 = _fetcher.CallDeriveFingerprint($"ssh-rsa X {b64b}");

            Assert.That(r1, Is.Not.EqualTo(r2));
        }
    }


    // ?? AcceptHostKey ?????????????????????????????????????????????????????????
    [TestFixture]
    public class TestAcceptHostKey
    {
        private string _tmpConfig = null!;

        [SetUp]
        public void SetUp()
        {
            _tmpConfig = Path.Combine(Path.GetTempPath(), "sparkleshare-tests-" + Path.GetRandomFileName());
            Directory.CreateDirectory(_tmpConfig);

            // Point Configuration.DefaultConfiguration to our temp directory
            // by overwriting the internal field via reflection
            var field = typeof(Configuration).GetField(
                "_defaultConfiguration",
                BindingFlags.NonPublic | BindingFlags.Static);

            if (field != null)
            {
                // Configuration(string config_path, string config_file_name)
                var cfg = new Configuration(_tmpConfig, "config.xml");
                field.SetValue(null, cfg);
            }
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tmpConfig))
                Directory.Delete(_tmpConfig, recursive: true);
        }

        private StubSSHFetcher CreateFetcher(string host = "github.com")
        {
            var info = FetcherInfoFactory.Create(address: $"ssh://git@{host}");
            return new StubSSHFetcher(info);
        }

        [Test]
        public void CreatesKnownHostsFileIfMissing()
        {
            var fetcher = CreateFetcher();

            // AcceptHostKey always uses Configuration.DefaultConfiguration.DirectoryPath
            // We verify that the call does not throw and known_hosts ends up somewhere
            // (we can only observe side-effects if we know the config path)
            string ssh_dir    = Path.Combine(Configuration.DefaultConfiguration.DirectoryPath, "ssh");
            string known_hosts = Path.Combine(ssh_dir, "known_hosts");

            // Remove if left over from previous run
            if (File.Exists(known_hosts)) File.Delete(known_hosts);
            if (Directory.Exists(ssh_dir)) Directory.Delete(ssh_dir, true);

            Assert.DoesNotThrow(() =>
                fetcher.CallAcceptHostKey("github.com ssh-rsa AAAAB3NzaC1", warn: false));

            Assert.That(File.Exists(known_hosts), Is.True);
        }

        [Test]
        public void AppendsHostKeyToKnownHostsFile()
        {
            var fetcher      = CreateFetcher();
            string ssh_dir   = Path.Combine(Configuration.DefaultConfiguration.DirectoryPath, "ssh");
            string known_hosts = Path.Combine(ssh_dir, "known_hosts");

            Directory.CreateDirectory(ssh_dir);
            File.WriteAllText(known_hosts, "");

            string host_key = "github.com ssh-rsa AAAAB3NzaC1yc2EAAAA";
            fetcher.CallAcceptHostKey(host_key, warn: false);

            string content = File.ReadAllText(known_hosts);
            Assert.That(content, Does.Contain(host_key));
        }

        [Test]
        public void DoesNotAddDuplicateHostKey()
        {
            var fetcher        = CreateFetcher("example.com");
            string known_hosts = Path.Combine(_tmpConfig, "ssh", "known_hosts");
            Directory.CreateDirectory(Path.GetDirectoryName(known_hosts)!);
            string host_key    = "example.com ssh-rsa AAAAB3NzaC1yc2E=";
            File.WriteAllText(known_hosts, host_key + "\n");

            fetcher.CallAcceptHostKey(host_key, warn: false);

            string content = File.ReadAllText(known_hosts);
            int count = CountOccurrences(content, host_key);
            Assert.That(count, Is.EqualTo(1), "Host key must not be duplicated");
        }

        [Test]
        public void AddsWarningWhenWarnIsTrue()
        {
            var fetcher        = CreateFetcher("warn.example.com");
            string known_hosts = Path.Combine(_tmpConfig, "ssh", "known_hosts");
            Directory.CreateDirectory(Path.GetDirectoryName(known_hosts)!);
            File.WriteAllText(known_hosts, "");

            string b64      = Convert.ToBase64String(new byte[] { 10, 20, 30, 40 });
            string host_key = $"warn.example.com ssh-rsa AAAA {b64}";

            fetcher.CallAcceptHostKey(host_key, warn: true);

            Assert.That(fetcher.Warnings, Has.Length.GreaterThan(0));
            Assert.That(fetcher.Warnings[0], Does.Contain("host key"));
        }

        [Test]
        public void NoWarningWhenWarnIsFalse()
        {
            var fetcher        = CreateFetcher("nowarn.example.com");
            string known_hosts = Path.Combine(_tmpConfig, "ssh", "known_hosts");
            Directory.CreateDirectory(Path.GetDirectoryName(known_hosts)!);
            File.WriteAllText(known_hosts, "");

            string host_key = "nowarn.example.com ssh-rsa AAAA AAEC";
            fetcher.CallAcceptHostKey(host_key, warn: false);

            Assert.That(fetcher.Warnings, Has.Length.EqualTo(0));
        }

        private static int CountOccurrences(string text, string pattern)
        {
            int count = 0, index = 0;
            while ((index = text.IndexOf(pattern, index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += pattern.Length;
            }
            return count;
        }
    }


    // ?? BaseFetcher ???????????????????????????????????????????????????????????
    [TestFixture]
    public class TestBaseFetcher
    {
        [Test]
        public void RemoteUrlIsBuiltCorrectlyFromAddressAndPath()
        {
            var info = FetcherInfoFactory.Create(
                address:    "ssh://git@github.com",
                remotePath: "/user/repo.git");

            var fetcher = new StubSSHFetcher(info);

            string url = fetcher.RemoteUrl.ToString();
            Assert.That(url, Does.Contain("github.com"));
            Assert.That(url, Does.Contain("repo.git"));
        }

        [Test]
        public void SshPrefixIsAddedWhenMissing()
        {
            var info = FetcherInfoFactory.Create(
                address:    "git@github.com",
                remotePath: "/user/repo.git");

            var fetcher = new StubSSHFetcher(info);

            Assert.That(fetcher.RemoteUrl.ToString(), Does.StartWith("ssh://"));
        }

        [Test]
        public void TrailingSlashInAddressIsStripped()
        {
            var info = FetcherInfoFactory.Create(
                address:    "ssh://git@github.com/",
                remotePath: "/user/repo.git");

            var fetcher = new StubSSHFetcher(info);

            Assert.That(fetcher.RemoteUrl.ToString(), Does.Not.Contain("//user"));
        }

        [Test]
        public void FetchPriorHistoryIsStoredCorrectly()
        {
            var info = FetcherInfoFactory.Create();
            info.FetchPriorHistory = true;

            var fetcher = new StubSSHFetcher(info);
            Assert.That(fetcher.FetchPriorHistory, Is.True);
        }

        [Test]
        public void RequiredFingerprintIsStoredCorrectly()
        {
            string fp   = "ab:cd:ef:12:34:56";
            var info    = FetcherInfoFactory.Create(fingerprint: fp);
            var fetcher = new StubSSHFetcher(info);

            Assert.That(fetcher.RequiredFingerprint, Is.EqualTo(fp));
        }

        [Test]
        public void IsActiveIsFalseAfterConstruction()
        {
            var fetcher = new StubSSHFetcher(FetcherInfoFactory.Create());
            Assert.That(fetcher.IsActive, Is.False);
        }

        [Test]
        public void AvailableStorageTypesContainsPlain()
        {
            var fetcher = new StubSSHFetcher(FetcherInfoFactory.Create());
            Assert.That(fetcher.AvailableStorageTypes, Has.Count.GreaterThan(0));
            Assert.That(fetcher.AvailableStorageTypes[0].Type, Is.EqualTo(StorageType.Plain));
        }

        [Test]
        public void WarningsAndErrorsAreEmptyAfterConstruction()
        {
            var fetcher = new StubSSHFetcher(FetcherInfoFactory.Create());
            Assert.That(fetcher.Warnings, Is.Empty);
            Assert.That(fetcher.Errors,   Is.Empty);
        }

        [Test]
        public void FormatNameReturnsRepoName()
        {
            var info    = FetcherInfoFactory.Create(remotePath: "/user/my-repo.git");
            var fetcher = new StubSSHFetcher(info);

            Assert.That(fetcher.FormatName(), Is.EqualTo("my-repo.git"));
        }

        [Test]
        public void GetBackendReturnsGitForStandardUrl()
        {
            string backend = BaseFetcher.GetBackend("ssh://git@github.com/user/repo.git");
            Assert.That(backend, Is.EqualTo("Git"));
        }

        [Test]
        public void GetBackendExtractsCustomBackend()
        {
            string backend = BaseFetcher.GetBackend("ssh+custom://git@host/path");
            Assert.That(backend, Is.EqualTo("Custom"));
        }

        [Test]
        public void StopSetsStopCalledFlag()
        {
            var fetcher = new StubSSHFetcher(FetcherInfoFactory.Create());
            fetcher.Stop();
            Assert.That(fetcher.StopWasCalled, Is.True);
        }
    }


    // ?? SparkleFetcherInfo ????????????????????????????????????????????????????
    [TestFixture]
    public class TestSparkleFetcherInfo
    {
        [Test]
        public void DefaultFetchPriorHistoryIsFalse()
        {
            var info = new SparkleFetcherInfo();
            Assert.That(info.FetchPriorHistory, Is.False);
        }

        [Test]
        public void CanSetAndReadAllProperties()
        {
            var info = new SparkleFetcherInfo
            {
                Address          = "ssh://git@example.com",
                RemotePath       = "/repo.git",
                Fingerprint      = "ab:cd",
                Backend          = "Git",
                TargetDirectory  = "/tmp/repo",
                FetchPriorHistory = true,
                AnnouncementsUrl = "https://announce.example.com"
            };

            Assert.Multiple(() =>
            {
                Assert.That(info.Address,          Is.EqualTo("ssh://git@example.com"));
                Assert.That(info.RemotePath,       Is.EqualTo("/repo.git"));
                Assert.That(info.Fingerprint,      Is.EqualTo("ab:cd"));
                Assert.That(info.Backend,          Is.EqualTo("Git"));
                Assert.That(info.TargetDirectory,  Is.EqualTo("/tmp/repo"));
                Assert.That(info.FetchPriorHistory,Is.True);
                Assert.That(info.AnnouncementsUrl, Is.EqualTo("https://announce.example.com"));
            });
        }
    }
}

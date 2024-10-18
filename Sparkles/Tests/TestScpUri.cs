using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sparkles.Tests
{
    [TestFixture()]
    internal class TestScpUri
    {
        static string urlString = "git@github.com:uenz/SparkleShare.git";
        static ScpUri scp_url = new(urlString);
        static ScpUri url = new(urlString,false);

        [Test()]
        public void ScpUriToString()
        {
            //string urlString = "git@github.com:uenz/SparkleShare.git";
            //ScpUri scp_url= new(urlString);
            //ScpUri url = new(urlString,false);

            Assert.That(TestScpUri.scp_url.ToString()== TestScpUri.urlString);
            Assert.That(TestScpUri.url.ToString() == TestScpUri.url.Scheme+"://"+ TestScpUri.urlString.Replace(':','/'));
        }
        [Test()]
        public void SshUriToString()
        {
            string urlString = "ssh://git@github.com/uenz/SparkleShare.git";
            ScpUri scp_url = new(urlString);
            ScpUri url = new(urlString, false);

            Assert.That(scp_url.ToString() == urlString);
            Assert.That(url.ToString() == urlString);
        }
        [Test()]
        public void HttpToString()
        {
            string urlString = "http://git@github.com/uenz/SparkleShare.git";
            ScpUri scp_url = new(urlString);
            ScpUri url = new(urlString, false);

            Assert.That(scp_url.ToString() == urlString);
            Assert.That(url.ToString() == urlString);
        }
        [Test()]
        public void AbsolutePath()
        {
            Assert.That(TestScpUri.url.AbsolutePath == "/uenz/SparkleShare.git");
            Assert.That(TestScpUri.scp_url.AbsolutePath == ":uenz/SparkleShare.git");

        }
        [Test()]
        public void Scheme()
        {
            Assert.That(TestScpUri.url.Scheme == TestScpUri.scp_url.Scheme);
        }
        [Test()]
        public void Host()
        {
            Assert.That(TestScpUri.url.Host == TestScpUri.scp_url.Host);
        }
        [Test()]
        public void Userinfo()
        {
            Assert.That(TestScpUri.url.UserInfo == TestScpUri.scp_url.UserInfo);
        }
    }
}

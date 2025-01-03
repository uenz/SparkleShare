//   SparkleShare, a collaboration and sharing tool.
//   Copyright (C) 2010  Hylke Bons (hi@planetpeanut.uk)
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
//   along with this program. If not, see (http://www.gnu.org/licenses/).


using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Xml;

namespace Sparkles
{

    public class SparkleInvite : XmlDocument
    {

        public string Address { get; private set; } = null!;
        public string RemotePath { get; private set; } = null!;
        public string Fingerprint { get; private set; } = null!;
        public string AcceptUrl { get; private set; } = null!;
        public string AnnouncementsUrl { get; private set; } = null!;

        public bool IsValid
        {
            get
            {
                return (!string.IsNullOrEmpty(Address) && !string.IsNullOrEmpty(RemotePath));
            }
        }


        public SparkleInvite(string xml_file_path)
        {
            try
            {
                Load(xml_file_path);

            }
            catch (XmlException e)
            {
                Logger.LogInfo("Invite", "Error parsing XML", e);
                return;
            }

            Address = ReadField("address");
            RemotePath = ReadField("remote_path");
            AcceptUrl = ReadField("accept_url");
            AnnouncementsUrl = ReadField("announcements_url");
            Fingerprint = ReadField("fingerprint");
        }


        public bool Accept(string public_key)
        {
#if __MonoCS__
            ServicePointManager.ServerCertificateValidationCallback = delegate {
                return true;
            };
#endif

            if (string.IsNullOrEmpty(AcceptUrl))
                return true;

            string post_data = "public_key=" + Uri.EscapeDataString(public_key);
            byte[] post_bytes = Encoding.UTF8.GetBytes(post_data);
            HttpResponseMessage response = null!;            
            HttpClient client=new();
            try
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));
                response = client.PostAsync(AcceptUrl, new StringContent(post_data)).GetAwaiter().GetResult();
                var content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult(); ;
            }
            catch (WebException e)
            {
                Logger.LogInfo("Invite", "Failed uploading public key to " + AcceptUrl + "", e);
                return false;
            }

            if (response != null && response.StatusCode == HttpStatusCode.OK)
            {
                Logger.LogInfo("Invite", "Uploaded public key to " + AcceptUrl);
                return true;
            }

            return false;
        }


        string ReadField(string name)
        {
            try
            {
                XmlNode? node = SelectSingleNode("/sparkleshare/invite/" + name + "/text()");

                if (node != null)
                    return node.Value!;

                return "";

            }
            catch (XmlException e)
            {
                Logger.LogInfo("Invite", "Error reading field '" + name + "'", e);
                return "";
            }
        }
    }
}

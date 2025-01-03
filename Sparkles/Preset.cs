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
using System.Xml;
using System.Xml.Linq;

using IO = System.IO;

namespace Sparkles
{

    public class Preset : XmlDocument
    {

        public static string PresetsPath = "";

        public static string LocalPresetsPath = IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "org.sparkleshare.SparkleShare", "presets");

        new public string Name { get { return GetValue("info", "name")!; } }
        public string Description { get { return GetValue("info", "description")!; } }
        public string Backend { get { return GetValue("info", "backend")!; } }
        public string Fingerprint { get { return GetValue("info", "fingerprint")!; } }
        public string AnnouncementsUrl { get { return GetValue("info", "announcements_url")!; } }
        public string Address { get { return GetValue("address", "value")!; } }
        public string AddressExample { get { return GetValue("address", "example")!; } }
        public string Path { get { return GetValue("path", "value")!; } }
        public string PathExample { get { return GetValue("path", "example")!; } }

        public string ImagePath
        {
            get
            {
                string image_file_name = GetValue("info", "icon")!;
                string image_path = IO.Path.Combine(preset_directory, image_file_name);

                if (IO.File.Exists(image_path))
                    return image_path;

                return IO.Path.Combine(PresetsPath, image_file_name);
            }
        }

        public bool PathUsesLowerCase
        {
            get
            {
                string uses_lower_case = GetValue("path", "uses_lower_case")!;

                if (!string.IsNullOrEmpty(uses_lower_case))
                    return uses_lower_case.Equals(bool.TrueString);
                else
                    return false;
            }
        }

        string preset_directory;


        public Preset(string preset_path)
        {
            preset_directory = IO.Path.GetDirectoryName(preset_path)!;
            Load(preset_path);
        }


        public static Preset? Create(string name, string description, string address_value,
            string address_example, string path_value, string path_example)
        {
            string preset_path = IO.Path.Combine(LocalPresetsPath, name + ".xml");

            if (IO.File.Exists(preset_path))
                return null;

            string icon_name = "own-server.png";

            XElement xml =
                new XElement("sparkleshare",
                    new XElement("preset",
                        new XElement("info",
                            new XElement("name", name),
                            new XElement("description", description),
                            new XElement("icon", icon_name)
                        ),
                        new XElement("address",
                            new XElement("value", address_value),
                            new XElement("example", address_example)
                        ),
                        new XElement("path",
                            new XElement("value", path_value),
                            new XElement("example", path_example)
                        )
                    )
                );

            if (!IO.Directory.Exists(LocalPresetsPath))
                IO.Directory.CreateDirectory(LocalPresetsPath);

            IO.File.WriteAllText(preset_path, xml.ToString());
            return new Preset(preset_path);
        }


        private string? GetValue(string a, string b)
        {
            XmlNode? node = SelectSingleNode("/sparkleshare/preset/" + a + "/" + b + "/text()");

            if (node != null && !string.IsNullOrEmpty(node.Value))
                return node.Value;
            else
                return null;
        }
    }
}

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
using System.Collections.Generic;

namespace Sparkles
{

    public enum ChangeType
    {
        Added,
        Edited,
        Deleted,
        Moved
    }


    public class ChangeSet
    {

        public User User = new User("Unknown", "Unknown");

        public SparkleFolder Folder = null!;
        public string Revision = null!;
        public DateTime Timestamp;
        public DateTime FirstTimestamp;
        public ScpUri RemoteUrl = null!;

        public List<Change> Changes = new List<Change>();

        public string ToMessage()
        {
            string message = "added: {0}";

            switch (Changes[0].Type)
            {
                case ChangeType.Edited: message = "edited: {0}"; break;
                case ChangeType.Deleted: message = "deleted: {0}"; break;
                case ChangeType.Moved: message = "moved: {0}"; break;
            }

            if (Changes.Count > 0)
                return string.Format(message, Changes[0].Path);
            else
                return "did something magical";
        }
    }


    public class Change
    {

        public ChangeType Type;
        public DateTime Timestamp;
        public bool IsFolder;

        public string Path = null!;
        public string MovedToPath = null!;
    }


    public class SparkleFolder
    {

        public string Name = null!;
        public Uri RemoteAddress = null!;

        public string FullPath
        {
            get
            {

                string? custom_path = Configuration.DefaultConfiguration.GetFolderOptionalAttribute(Name, "path");

                if (custom_path != null)
                    return Path.Combine(custom_path, Name);

                return Path.Combine(Configuration.DefaultConfiguration.FoldersPath,
                                        new ScpUri(Configuration.DefaultConfiguration.UrlByName(Name)!).Host,
                                        Name);
            }
        }


        public SparkleFolder(string name)
        {
            Name = name;
        }
    }


    public class Announcement
    {

        public readonly string FolderIdentifier;
        public readonly string Message;


        public Announcement(string folder_identifier, string message)
        {
            FolderIdentifier = folder_identifier;
            Message = message;
        }
    }
}

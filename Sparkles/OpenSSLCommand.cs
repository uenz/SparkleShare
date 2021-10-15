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


using System.IO;

namespace Sparkles
{
    public class OpenSSLCommand : Command
    {
        public static string OpenSSLPath = OpenSSLCommandPath; //TODO check
        public static string OpenSSLBinary = "openssl";

        public static string SSHPath = "\"" + Path.GetFullPath(LocateCommand(OpenSSLBinary)).Replace("\\", "/") + "\"";
        public static string OpenSSLCommandPath
        {
            get
            {
                return "\"" + LocateCommand(OpenSSLBinary).Replace("\\", "/") + "\"";
            }
        }


        public OpenSSLCommand(string command, string args) :
            base(Path.Combine(OpenSSLPath, command), args)
        {
        }

    }
}

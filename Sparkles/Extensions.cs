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
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Security.Cryptography;
using System.Text;

namespace Sparkles
{

    public static class Extensions
    {

        public static string SHA256(this string s)
        {
            byte[] bytes = ASCIIEncoding.Default.GetBytes(s);
            return SHA256(bytes);
        }
        public static string SHA256(this byte[] bytes)
        {
            SHA256 sha256 = System.Security.Cryptography.SHA256.Create();
            byte[] sha256_bytes = sha256.ComputeHash(bytes);

            return BitConverter.ToString(sha256_bytes).ToLower().Replace("-", "");
        }

        public static string SHA256(this string s, string salt)
        {
            SHA256 sha256 = System.Security.Cryptography.SHA256.Create();
            byte[] bytes = ASCIIEncoding.Default.GetBytes(s + salt);
            byte[] sha256_bytes = sha256.ComputeHash(bytes);

            return BitConverter.ToString(sha256_bytes).ToLower().Replace("-", "");
        }


        public static string MD5(this string s)
        {
            MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] bytes = ASCIIEncoding.Default.GetBytes(s);
            byte[] md5_bytes = md5.ComputeHash(bytes);

            return BitConverter.ToString(md5_bytes).ToLower().Replace("-", "");
        }

        public static string AESEncrypt(this string plain_text, string password)
        {
            Random random = new Random();
            byte[] salt_bytes = new Byte[16];
            random.NextBytes(salt_bytes);

            string salt = Convert.ToBase64String(salt_bytes);
            password = (password + salt).SHA256().Substring(0, 32);

            var aes = Aes.Create();
            aes.KeySize = 256;
            aes.BlockSize = 128;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = Encoding.UTF8.GetBytes(password);
            aes.IV = Encoding.UTF8.GetBytes(password.ToCharArray(), 0, 16);


            byte[] buffer = Encoding.UTF8.GetBytes(plain_text);
            ICryptoTransform crypto = aes.CreateEncryptor(aes.Key, aes.IV);
            byte[] encrypted_bytes = crypto.TransformFinalBlock(buffer, 0, buffer.Length);

            return salt + "_" + Convert.ToBase64String(encrypted_bytes);
        }

        public static string AESDecrypt(this string s, string password)
        {
            string salt = s.Substring(0, s.IndexOf("_"));
            password = (password + salt).SHA256().Substring(0, 32);

            var aes = Aes.Create();
            aes.KeySize = 256;
            aes.BlockSize = 128;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = Encoding.UTF8.GetBytes(password);
            aes.IV = Encoding.UTF8.GetBytes(password.ToCharArray(), 0, 16);

            string encrypted_text = s.Substring(s.IndexOf("_") + 1);
            byte[] buffer = Convert.FromBase64String(encrypted_text);
            ICryptoTransform crypto = aes.CreateDecryptor(aes.Key, aes.IV);
            byte[] decrypted_bytes = crypto.TransformFinalBlock(buffer, 0, buffer.Length);

            return Encoding.UTF8.GetString(decrypted_bytes);
        }


        // Format a file size nicely with small caps.
        // Example: 1048576 becomes "1 ᴍʙ"
        public static string ToSize(this double byte_count)
        {
            if (byte_count >= 1099511627776)
                return string.Format("{0:##.##} ᴛʙ", Math.Round(byte_count / 1099511627776, 2));
            else if (byte_count >= 1073741824)
                return string.Format("{0:##.##} ɢʙ", Math.Round(byte_count / 1073741824, 1));
            else if (byte_count >= 1048576)
                return string.Format("{0:##.##} ᴍʙ", Math.Round(byte_count / 1048576, 1));
            else if (byte_count >= 1024)
                return string.Format("{0:##.##} ᴋʙ", Math.Round(byte_count / 1024, 0));
            else
                return byte_count + " ʙ";
        }


        public static bool IsSymlink(this string path)
        {
            var file = new FileInfo(path);
            return ((file.Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint);
        }


        public static string ToPrettyDate(this DateTime timestamp)
        {
            TimeSpan time_diff = DateTime.Now.Subtract(timestamp);
            var day_diff = (int)time_diff.TotalDays;
            DateTime yesterday = DateTime.Today.AddDays(-1);

            if (timestamp >= yesterday && timestamp < DateTime.Today)
            {
                return "yesterday at " + timestamp.ToString("HH:mm");

            }
            else if (day_diff == 0)
            {
                return "today at " + timestamp.ToString("HH:mm");

            }
            else if (day_diff < 7)
            {
                return timestamp.ToString("dddd");

            }
            else if (day_diff < 31)
            {
                if (day_diff < 14)
                    return "a week ago";
                else
                    return string.Format("{0} weeks ago", Math.Ceiling((double)day_diff / 7));

            }
            else if (day_diff < 62)
            {
                return "a month ago";

            }
            else
            {
                return string.Format("{0} months ago", Math.Ceiling((double)day_diff / 31));
            }
        }


        public static string ReplaceUnderscoreWithSpace(this string s)
        {
            int len = s.Length, lead = 0, trail = 0;
            for (int i = 0; i < len && s[i] == '_'; i++, lead++)
                ; // nop
            for (int i = len - 1; i >= lead && s[i] == '_'; i--, trail++)
                ; // nop
            if (lead == 0 && trail == 0)
                return s.Replace("_", " "); // fast code path
            else
                return s.Substring(0, lead) +
                       s.Substring(lead, len - lead - trail).Replace("_", " ") +
                       s.Substring(len - trail, trail);
        }
    }
}

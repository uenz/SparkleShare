using System.Diagnostics.CodeAnalysis;

namespace Sparkles
{
    public class ScpUri

    {
        public bool scp_style = false;
        protected Uri uri = null!;

        public ScpUri([StringSyntax("Uri")] string uriString)

        {
            if (Uri.TryCreate(uriString, UriKind.RelativeOrAbsolute, out uri!) && uriString.Contains("://"))
            {
                scp_style = false;
            }
            else
            {
                if (!uriString.Contains("://"))
                {
                    uriString = "ssh://" + uriString;
                }
                int place = uriString.LastIndexOf(':');
                if (place != -1)

                {
                    uriString = uriString.Remove(place, 1).Insert(place, "/");
                }

                Uri.TryCreate(uriString, UriKind.RelativeOrAbsolute, out uri!);
                scp_style = true;
            }
        }

        public ScpUri([StringSyntax("Uri")] string uriString, bool use_scp_style) : this(uriString)
        {
            scp_style = use_scp_style;
        }

        public string AbsolutePath

        {
            get

            {
                if (scp_style == false)
                {
                    return uri.AbsolutePath;
                }
                else
                {
                    string tmp = this.ToString();
                    return tmp.Substring(tmp.LastIndexOf(':'));
                }
            }
        }

        public string Host
        {
            get
            {
                return uri.Host;
            }
        }

        public int Port

        {
            get
            {
                if (scp_style == false)
                    return uri.Port;
                else
                    return -1;
            }
        }

        public string Scheme

        {
            get

            {
                return uri.Scheme;
            }
        }
        public string UserInfo
        {
            get
            {
                return uri.UserInfo;
            }
        }
        override public string ToString()

        {
            if (scp_style == false)
            {
                return uri.ToString();

            }
            else
            {
                // For SCP style: user@host:path (scheme needs to be removed)
                string result = uri.ToString();
                
                // Remove scheme (ssh://)
                result = result.Replace(uri.Scheme + "://", "");
                
                // Ensure there's a colon after the host
                // Replace "host/" with "host:" if present
                if (result.Contains(uri.Host + "/"))
                {
                    result = result.Replace(uri.Host + "/", uri.Host + ":");
                }
                // If no slash after host, add colon directly
                else if (!result.Contains(uri.Host + ":"))
                {
                    result = result.Replace(uri.Host, uri.Host + ":");
                }
                
                return result;
            }
        }
        public string ToUriString()

        {
            return uri.ToString();
        }
    }
}
using System;
using System.IO;
using System.Net;
using System.Text;

namespace ScanSRV
{
    class Web
    {
        public static string GetHTTPfromURL(string url)
        {
            ServicePointManager.DefaultConnectionLimit = 2;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Proxy = null;
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            string sData = "";
            if (response.StatusCode == HttpStatusCode.OK)
            {
                Stream receiveStream = response.GetResponseStream();
                StreamReader readStream = null;

                if (response.CharacterSet == null)
                {
                    readStream = new StreamReader(receiveStream);
                }
                else
                {
                    if (response.CharacterSet == "ISO-8859-1")
                        readStream = new StreamReader(receiveStream, Encoding.GetEncoding("UTF-8"));
                    else
                        readStream = new StreamReader(receiveStream, Encoding.GetEncoding(response.CharacterSet));
                }

                string data = readStream.ReadToEnd();
                sData = data;
                response.Close();
                readStream.Close();
            }
            return sData;
        }

        public static string GetNameFromHTTP(string data)
        {
            string name = "";
            int Pletter = data.IndexOf("personaname");
            if (Pletter != -1)
            {
                for (int i = Pletter + 14; i < data.Length; ++i)
                {
                    if ((int)data[i] == 34) // quotation mark "
                        break;
                    name += data[i];
                }
            }
            return name;
        }

        public static string SIDtoCom(string sid)
        {
            string community = "";
            char A = sid[8];
            string B = "";
            for (int i = 10; i < (int)sid.Length; ++i)
            {
                B += sid[i];
            }
            Int64 icom = Int64.Parse(B) * 2 + Int64.Parse(A.ToString()) + 76561197960265728;
            community = icom.ToString();
            return community;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace spider
{
    class Spider
    {
        public struct LinkInfo
        {
            public uint Deep { get; set; }
            public HashSet<string> Links { get; set; }
        }
        public HashSet<string> Links { get; private set; }
        public Dictionary<string, LinkInfo> SearchInfos { get; private set; }
        public void GetHtmlByWebRequest(string url, uint deep, ref ICollection<string> storeSubLink )
        {
            WebRequest request = WebRequest.Create(url);
            request.Credentials = CredentialCache.DefaultCredentials;
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            if (response.StatusDescription.ToUpper() != "OK") return;
            Encoding encoding;
            switch (response.CharacterSet?.ToLower())
            {
                case "gbk":
                    encoding = Encoding.GetEncoding("GBK");
                    break;
                case "gb2312":
                    encoding = Encoding.GetEncoding("GB2312");
                    break;
                case "big5":
                    encoding = Encoding.GetEncoding("Big5");
                    break;
                case "iso-8859-1":
                    encoding = Encoding.UTF8;
                    break;
                default:
                    encoding = Encoding.UTF8;
                    break;
            }

            Stream dataStream = response.GetResponseStream();
            if (dataStream != null)
            {
                StreamReader reader = new StreamReader(dataStream, encoding);
                string responseFromServer = reader.ReadToEnd();
                FindLink(url, responseFromServer, deep, ref storeSubLink);

                reader.Close();
                dataStream.Close();
            }
            else
            {
                throw new InvalidDataException("Cannot get response stream.");
            }
        }
        private void FindLink(string srcUrl, string html, uint deep, ref ICollection<string> storeSubLink)
        {
            Regex hrefPattern = new Regex(@"<a.*?href=(""|')(?<href>[\s\S.]*?)(""|').*?>");
            MatchCollection matchCollection = hrefPattern.Matches(html);
            Regex linkPattern = new Regex(@"^(\w*?://)?([\w-]+\.)+[\w-]+(/[\w- ./?%&=]*)?");
            foreach (Match match in matchCollection.Cast<Match>().Where(match => match.Success && linkPattern.IsMatch(match.Groups["href"].Value)))
            {
                if (!SearchInfos.ContainsKey(match.Groups["href"].Value))
                {
                    storeSubLink.Add(match.Groups["href"].Value);
                }
            }
        }

        public void Start(string url, uint deepLimit)
        {
            SearchInfos = new Dictionary<string, LinkInfo>();
            SearchInfos.Add(url, new LinkInfo() { Deep = 0 });
            foreach (var info in SearchInfos)
            {
                if (info.Value.Deep < deepLimit && info.Value.Links != null)
                {
                    GetHtmlByWebRequest(info.Key, info.Value.Deep, ref info.Value.Links);
                    foreach (string link in info.Value.Links)
                    {
                        GetHtmlByWebRequest(link, info.Value.Deep);
                    }
                }
            }
        }
    }
    class Program
    {
        static void Main()
        {
            Spider spider = new Spider();
            spider.Start("http://www.baidu.com", 10);
            foreach (var info in spider.SearchInfos)
            {
                Console.WriteLine($"In Link {info.Key}:");
                foreach (string subLink in info.Value.Links)
                {
                    Console.WriteLine($"\t -> {subLink}");
                }
            }
        }
    }
}

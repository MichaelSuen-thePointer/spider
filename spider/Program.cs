using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace spider
{
    class Spider
    {
        public string ResultText { get; private set; }
        public string Literal { get; private set; }
        public string Response { get; private set; }
        public List<string> LinkList { get; private set; }
        private string GetHtml(string urlString, Encoding encoding)
        {
            Url url = new Url(urlString);
            WebClient client = new WebClient();
            client.Encoding = encoding;
            Stream stream = client.OpenRead(urlString);
            if (stream != null)
            {
                StreamReader streamReader = new StreamReader(stream, encoding);
                return streamReader.ReadToEnd();
            }
            else
            {
                throw new InvalidDataException("Cannot get data stream.");
            }
        }
        public void GetHtmlByWebRequest(string url)
        {
            WebRequest request = WebRequest.Create(url);
            request.Credentials = CredentialCache.DefaultCredentials;
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            if (response.StatusDescription.ToUpper() == "OK")
            {
                Encoding encoding;
                switch (response.CharacterSet.ToLower())
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
                Literal = "Length:" + response.ContentLength.ToString() + "<br>CharacterSet:" + response.CharacterSet +
                          "<br>Header:" + response.Headers + "<br>";
                Stream dataStream = response.GetResponseStream();
                if (dataStream != null)
                {
                    StreamReader reader = new StreamReader(dataStream, encoding);
                    string responseFromServer = reader.ReadToEnd();
                    Response = responseFromServer;
                    FindLink(responseFromServer);
                    ResultText = ClearHtml(responseFromServer);

                    reader.Close();
                    dataStream.Close();
                    response.Close();
                }
                else
                {
                    throw new InvalidDataException("Cannot get response stream.");
                }
            }
            else
            {
                this.ResultText = "Error.";
            }
        }
        private void FindLink(string Html)
        {
            this.LinkList = new List<string>();
            string pattern = @"<a\s*href=(""|')(?<href>[\s\S.]*?)(""|').*?>\s*(?<name>[\s\S.]*?)</a>";
            MatchCollection matchCollection = Regex.Matches(Html, pattern);
            foreach (Match match in matchCollection)
            {
                if (match.Success)
                {
                    this.LinkList.Add($"{match.Groups["href"].Value} | {match.Groups["names"].Value}");
                }
            }
        }
        public string ClearHtml(string text)//过滤html,js,css代码
        {
            text = text.Trim();
            if (string.IsNullOrEmpty(text))
                return string.Empty;
            text = Regex.Replace(text, "<head[^>]*>(?:.|[\r\n])*?</head>", "");
            text = Regex.Replace(text, "<script[^>]*>(?:.|[\r\n])*?</script>", "");
            text = Regex.Replace(text, "<style[^>]*>(?:.|[\r\n])*?</style>", "");

            text = Regex.Replace(text, "(<[b|B][r|R]/*>)+|(<[p|P](.|\\n)*?>)", ""); //<br> 
            text = Regex.Replace(text, "\\&[a-zA-Z]{1,10};", "");
            text = Regex.Replace(text, "<[^>]*>", "");

            text = Regex.Replace(text, "(\\s*&[n|N][b|B][s|S][p|P];\\s*)+", ""); //&nbsp;
            text = Regex.Replace(text, "<(.|\\n)*?>", string.Empty); //其它任何标记
            text = Regex.Replace(text, "[\\s]{2,}", " "); //两个或多个空格替换为一个

            text = text.Replace("'", "''");
            text = text.Replace("\r\n", "");
            text = text.Replace("  ", "");
            return text.Trim();
        }
        private void IPAddresses(string url)
        {
            url = url.Substring(url.IndexOf("//") + 2);
            if (url.IndexOf("/") != -1)
            {
                url = url.Remove(url.IndexOf("/"));
            }
            Literal += "<br>" + url;
            try
            {
                System.Text.ASCIIEncoding ASCII = new System.Text.ASCIIEncoding();
                IPHostEntry ipHostEntry = Dns.GetHostEntry(url);
                System.Net.IPAddress[] ipaddress = ipHostEntry.AddressList;
                foreach (IPAddress item in ipaddress)
                {
                    Literal += "IP:" + item;
                }
            }
            catch
            {

            }
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            Spider spider = new Spider();
            spider.GetHtmlByWebRequest(@"http://www.baidu.com");
        }
        
    }
}

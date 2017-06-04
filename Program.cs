using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using Facebook;
using HtmlAgilityPack;

namespace FoodParser
{
    public class Program
    {
        private static bool _isDevelopment = true;

        static void Main(string[] args)
        {
            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => { return true; };

            string subject = string.Format("Dnevni menu za {0}", DateTime.Today.ToString("dd.MM.yyyy"));
            string doubleNewLine = Environment.NewLine + Environment.NewLine;
            var body = new StringBuilder(
                string.Format("Pozdravljeni, {0}{1}", doubleNewLine,
                    string.Format("{0}{1}", "Napočila je 10.30 ura, kar pomeni,",
                        string.Format(
                            " da se bliža malica. Moja dolžnost je, da Vam za dan, {0}, v emailu pošljem dnevne menije vaših najljubših restavracij: ",
                            DateTime.Today.ToLongDateString()))));
            try
            {
                body.Append(string.Format("{0}{1}{0}{2}", doubleNewLine, GetBarbadoMenu(), Environment.NewLine));
            }
            catch (Exception ex)
            {
                body.AppendLine(ex.ToString());
            }

            try
            {
                body.Append(string.Format("<br>{1}{0}", Environment.NewLine, GetFavolaMenu()));
            }
            catch (Exception ex)
            {
                body.AppendLine(ex.ToString());
            }

            try
            {
                body.Append(string.Format("{1}{0}", Environment.NewLine, GetPiapMenu()));
            }
            catch (Exception ex)
            {
                body.AppendLine(ex.ToString());
            }

            try
            {
                body.Append(string.Format("<b>Tiskarna:</b>{0}{1}{2}", doubleNewLine, GetTiskarnaMenu(), doubleNewLine));
            }
            catch (Exception ex)
            {
                body.AppendLine(ex.ToString());
            }

            body.AppendLine(
                string.Format("<hr>{1}{0}{1}",
                    string.Format("Želim Vam dober tek. Kot pravi stari Slovenski pregovor: {0}<b>{1}</b>",
                        doubleNewLine, Proverb.GetRandomProverb()),
                    Environment.NewLine));
            body.Append(
                string.Format(
                    @"Lep pozdrav, {0}<b><font size = ""6"" color = ""blue"">MonoMenu</font></b> (<a href=""https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=YHDH5LFQB89WG"">doniraj za podporo novih restavracij?</a>)",
                    doubleNewLine));
            SendMail(subject, body.ToString());
            Console.ReadLine();
        }

        public static string GetBarbadoMenu()
        {
            var doc = new HtmlDocument
            {
                OptionFixNestedTags = true,
                OptionDefaultStreamEncoding = Encoding.UTF8,
                OptionAutoCloseOnEnd = true
            };

            string htmlString;
            using (var client = new WebClient())
            {
                client.Encoding = Encoding.UTF8;
                htmlString = client.DownloadString("http://www.barbado.si/");
            }

            int startIndex = htmlString.LastIndexOf(@"<div class=""text_exposed_root text_exposed"">",
                StringComparison.Ordinal);
            int endIndex = startIndex + htmlString.Substring(startIndex).IndexOf(@"</div>", StringComparison.Ordinal) +
                           6;

            string menuHtml = htmlString.Substring(startIndex, endIndex - startIndex);

            doc.LoadHtml(menuHtml);

            var sb = new StringBuilder("<b>Barbado:</b>" + Environment.NewLine + Environment.NewLine);
            foreach (
                HtmlNode childNode in doc.DocumentNode.ChildNodes.FirstOrDefault().ChildNodes.Where(x => x.Name == "p"))
            {
                sb.AppendLine(string.Format("<li>{0}</li>", childNode.InnerText));
            }

            sb = sb.Remove(sb.Length - 24, 24);
            return sb.ToString();
        }

        public static string GetFavolaMenu()
        {
            var doc = new HtmlDocument
            {
                OptionFixNestedTags = true,
                OptionDefaultStreamEncoding = Encoding.GetEncoding("windows-1250"),
                OptionAutoCloseOnEnd = true
            };

            string htmlString;
            using (var client = new WebClient())
            {
                client.Encoding = Encoding.GetEncoding("windows-1250");
                htmlString = client.DownloadString("http://www.kaval-group.si/FAVOLA,,ponudba/kosila");
            }

            doc.LoadHtml(htmlString);

            HtmlNodeCollection results = doc.DocumentNode.SelectNodes(string.Format("//*[contains(@class,'{0}')]", "childNaviLiElement"));

            foreach (HtmlNode item in results.Where(item => item.FirstChild.Attributes["onclick"] != null))
            {
                if (item.InnerText.Contains(string.Format("{0}", DateTime.Now.ToString("d.M.yyyy"))) ||
                    item.InnerText.Contains(string.Format("{0}", DateTime.Now.ToString("d.MM.yyyy"))) ||
                    item.InnerText.Contains(string.Format("{0}", DateTime.Now.ToString("dd.M.yyyy"))) ||
                    item.InnerText.Contains(string.Format("{0}", DateTime.Now.ToString("dd.MM.yyyy"))))
                {
                    //PrintHtml(item);

                    string className = string.Format("show show-{0}", item.FirstChild.Attributes["class"].Value.Split('-').LastOrDefault());
                    HtmlNode activeMenu = doc.DocumentNode.SelectSingleNode(string.Format("//*[contains(@class,'{0}')]", className));
                    var sb = new StringBuilder("<b>Favola:</b>" + Environment.NewLine + Environment.NewLine);
                    foreach (
                        HtmlNode childNode in activeMenu.ChildNodes.Where(x => x.Name == "p"))
                    {
                        if (!childNode.InnerText.Contains("***"))
                        {
                            sb.AppendLine(string.Format("<li>{0}</li>", childNode.InnerText));
                        }
                    }

                    return sb.ToString();
                    //PrintHtml(activeMenu);
                }
            }

            return "";
        }

        public static string GetPiapMenu()
        {
            var doc = new HtmlDocument
            {
                OptionFixNestedTags = true,
                OptionDefaultStreamEncoding = Encoding.UTF8,
                OptionAutoCloseOnEnd = true
            };

            string htmlString;
            using (var client = new WebClient())
            {
                client.Encoding = Encoding.UTF8;
                htmlString = client.DownloadString("http://www.piap.si");
            }

            doc.LoadHtml(htmlString);

            HtmlNodeCollection results = doc.DocumentNode.SelectNodes(string.Format("//*[contains(@class,'{0}')]", "menu_desno_polje"));
            var sb = new StringBuilder("<b>Piap:</b>" + Environment.NewLine + Environment.NewLine);
            foreach (HtmlNode item in results)
            {
                sb.AppendLine(string.Format("<li>{0}</li>", FirstLetterToUpper(item.InnerText)));
            }

            return sb.ToString();
        }

        private static string FirstLetterToUpper(string str)
        {
            if (str == null)
                return null;

            if (str.Length > 1)
                return char.ToUpper(str[0]) + str.Substring(1).ToLower();

            return str.ToUpper();
        }

        private static void PrintHtml(HtmlNode item)
        {
            Debug.WriteLine("******************");
            Debug.WriteLine("Outerhtml");
            Debug.WriteLine(item.OuterHtml);
            Debug.WriteLine("Innerhtml");
            Debug.WriteLine(item.InnerHtml);
            Debug.WriteLine("Innertext");
            Debug.WriteLine(item.InnerText);
            Debug.WriteLine("");
        }

        private static string GetTiskarnaMenu()
        {
            //get token from https://developers.facebook.com/tools/explorer/
            //then open https://developers.facebook.com/tools/debug/accesstoken and extend its duration

            string accessToken = "ACCESS TOKEN";
            var fb = new FacebookClient(accessToken);
            var pageFeed = string.Format("/v2.4/{0}/feed", "kavarnaTiskarna");
            var response = fb.Get(pageFeed) as JsonObject;
            var allPosts = response?.Values.First() as JsonArray;
            if (allPosts != null)
            {
                var firstPost = allPosts.First() as JsonObject;
                return string.Format("<li>{0}</li>", firstPost?.Values.First());
            }

            return "Could not parse via Facebook API.";

        }

        private static void SendMail(string subject, string body)
        {
            string sender = "lacen@ti.si";
            string[] recipients;

            if (_isDevelopment)
            {
                recipients = new[] {"email@gmail.com"};
            }
            else
            {
                recipients = new[]
                {
                    "email@address.com"
                };
            }
            try
            {
                var mail = new MailMessage
                {
                    From = new MailAddress(sender, "Dnevni menu"),
                };
                foreach (var recipient in recipients)
                {
                    mail.Bcc.Add(recipient);
                }

                var client = new SmtpClient
                {
                    UseDefaultCredentials = false,
                    Port = 25,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Host = "SMTP URL"
                };

                mail.Subject = subject;
                mail.IsBodyHtml = true;
                mail.Body = body.Replace(Environment.NewLine, "<br>");
                client.Send(mail);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex); //Should print stacktrace + details of inner exception

                if (ex.InnerException != null)
                {
                    Console.WriteLine("InnerException is: {0}", ex.InnerException);
                }
            }
        }
    }

}

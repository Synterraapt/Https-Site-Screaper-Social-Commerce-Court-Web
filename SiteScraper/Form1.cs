using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace Web_Browser
{
    public partial class Form1 : Form
    {
        #region << Objects >>

        /*CSSObject Class 
         This is used to keep all the info for the css files*/
        public class CssMatcher
        {
            public string FileName { get; set; }
            public string FileLocation { get; set; }
        }

        /*JSObject Class 
         This is used to keep all the info for the js files*/
        public class JSMatcher
        {
            public string FileName { get; set; }
            public string FileLocation { get; set; }
        }
        /*this will be used for any other extensions we find in the site */
        public class OtherMatcher
        {
            public string FileName { get; set; }
            public string FileLocation { get; set; }
        }

        #endregion << Objects >>

        #region << Vars >>
        /*this is for url path*/
        String Url = string.Empty;

        /*error string builder for error /log file */
        string ErrorStr = "";


        /* list of objects */
        public List<CssMatcher> CSSList = new List<CssMatcher>();
        public List<JSMatcher> JSList = new List<JSMatcher>();
        public List<OtherMatcher> AllOtherFiles = new List<OtherMatcher>();
        public List<string> Pages = new List<string>();

        #endregion << Vars >>

        #region << Methods >>

        public Form1()
        {
            InitializeComponent();
            Url = ""; /*Set a URL here if need be*/
            //myBrowser();/*You can have the browser auto start here Uncomment to get it going*/
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void myBrowser()
        {
            if (toolStripComboBox1.Text != "")
                Url = toolStripComboBox1.Text;
            webBrowser1.Navigate(Url);
            webBrowser1.ProgressChanged += new WebBrowserProgressChangedEventHandler(webpage_ProgressChanged);
            webBrowser1.DocumentTitleChanged += new EventHandler(webpage_DocumentTitleChanged);
            webBrowser1.StatusTextChanged += new EventHandler(webpage_StatusTextChanged);
            webBrowser1.Navigated += new WebBrowserNavigatedEventHandler(webpage_Navigated);
            webBrowser1.DocumentCompleted += new WebBrowserDocumentCompletedEventHandler(webpage_DocumentCompleted);
        }

        /*Downloading File Function 
         This will download any file from any given url*/
        private void DownloadFile(string FromUrl, string ToLocation, string FileName)
        {
            try
            {
                byte[] data;
                using (WebClient client = new WebClient())
                {
                    /*add security protocols to our user*/
                    System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls | System.Net.SecurityProtocolType.Tls11 | System.Net.SecurityProtocolType.Tls12;
                    client.Headers.Add("user-agent", "Only a test!");/*most secure websites (https sites) just need a header for a user agent*/

                    /*Trim Replace and make everyhting more or less an easy download string*/
                    string urlfrom = FromUrl.Replace("../", "/").Replace("//", "/").Replace("\\", "/");
                    /*Decide if its https or http*/
                    if (urlfrom.Contains("https:/"))
                        urlfrom = urlfrom.Replace("https:/", "https://");
                    if (urlfrom.Contains("http:/"))
                        urlfrom = urlfrom.Replace("http:/", "http://");
                    /*create the download URL*/
                    Uri urltodownload = new Uri(urlfrom);
                    /*download all the data from the site*/
                    data = client.DownloadData(urltodownload);
                }
                /*create the working Directory*/
                if (!Directory.Exists(Application.StartupPath + @"\" + txtProjectName.Text.ToString()))
                {
                    Directory.CreateDirectory(Application.StartupPath + @"\" + txtProjectName.Text.ToString());
                }

                /*Get Spitted files*/
                string ActualtFileName = FileName.Split('/').Last();
                string[] actualfolder = FileName.Split('/');
                string lastpath = "";

                /*folder path builder*/
                for (int i = 0; i < actualfolder.Length - 1; i++)
                {
                    lastpath = lastpath + @"\" + actualfolder[i];
                    if (!Directory.Exists(Application.StartupPath + @"\" + txtProjectName.Text.ToString() + @"\" + lastpath))
                    {
                        Directory.CreateDirectory(Application.StartupPath + @"\" + txtProjectName.Text.ToString() + @"\" + lastpath);
                    }

                }
                /*Write all bytes to the file where are downloading */
                File.WriteAllBytes(Application.StartupPath + @"\" + txtProjectName.Text.ToString() + @"\" + FileName.Replace("/", @"\"), data);

                /*if css or js or custom extension add it to the corresponding list*/
                if (ActualtFileName.Contains(".css"))
                {
                    CssMatcher csitem = new CssMatcher();
                    csitem.FileName = ActualtFileName;
                    csitem.FileLocation = Application.StartupPath + @"\" + txtProjectName.Text.ToString() + @"\" + FileName.Replace("/", @"\");
                    CSSList.Add(csitem);
                }
                if (ActualtFileName.Contains(".js"))
                {
                    JSMatcher jsitem = new JSMatcher();
                    jsitem.FileName = ActualtFileName;
                    jsitem.FileLocation = Application.StartupPath + @"\" + txtProjectName.Text.ToString() + @"\" + FileName.Replace("/", @"\");
                    JSList.Add(jsitem);
                }
                else
                {
                    OtherMatcher otehritem = new OtherMatcher();
                    otehritem.FileName = ActualtFileName;
                    otehritem.FileLocation = Application.StartupPath + @"\" + txtProjectName.Text.ToString() + @"\" + FileName.Replace("/", @"\");
                    AllOtherFiles.Add(otehritem);
                }

            }
            catch (Exception ex)
            {
                /*we dont want to log exeptions*/
            }

        }

        private string RetrunNewUrl(string Url)
        {
            var uri = new Uri(Url);

            var noLastSegment = string.Format("{0}://{1}", uri.Scheme, uri.Authority);

            for (int i = 0; i < uri.Segments.Length - 1; i++)
            {
                noLastSegment += uri.Segments[i];
            }

            return noLastSegment = noLastSegment.Trim("/".ToCharArray()); // remove trailing `/`
        }

        private void DownloadCurrentPage()
        {
            CSSList.Clear();
            JSList.Clear();
            AllOtherFiles.Clear();
            try
            {
                //this is for my testing 
                string HTMLCodeofsite;
                using (WebClient client = new WebClient())
                {
                    System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls | System.Net.SecurityProtocolType.Tls11 | System.Net.SecurityProtocolType.Tls12;
                    client.Headers.Add("user-agent", "Only a test!");
                    string urlfrom = Url.Replace("../", "/").Replace("//", "/").Replace("\\", "/");
                    if (urlfrom.Contains("https:/"))
                        urlfrom = urlfrom.Replace("https:/", "https://");
                    if (urlfrom.Contains("http:/"))
                        urlfrom = urlfrom.Replace("http:/", "http://");
                    Uri urltodownload = new Uri(urlfrom);
                    HTMLCodeofsite = client.DownloadString(urltodownload);
                }

                //HTMLCodeofsite = webBrowser1.DocumentText;
                Logger("Reading Page");
                if (HTMLCodeofsite != string.Empty)
                {
                    //we found the code now we mine 
                    /*now we read line by line*/
                    string[] array = HTMLCodeofsite.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                    foreach (String item in array)
                    {
                        //now we find the items we require

                        string[] lines = Regex.Split(item, "\"");
                        /* for HREF */
                        foreach (var lineitem in lines)
                        {
                            if (lineitem.Contains("css/") || lineitem.Contains("image/") || lineitem.Contains("images/") || lineitem.Contains("fonts/") || lineitem.Contains("font/") || lineitem.Contains("js/") || lineitem.Contains("script/"))
                            {
                                //clean the string a bit 
                                if (lineitem.Contains("url"))
                                {
                                    int pFrom = lineitem.IndexOf("url(");
                                    int pTo = lineitem.LastIndexOf(");");

                                    string results = lineitem.Substring(pFrom, pTo - pFrom).Replace("url(", "").Replace("../", "/");

                                    DownloadFile(RetrunNewUrl(Url).ToString() + @"\" + results, Application.StartupPath + @"\" + RetrunNewUrl(Url).ToString() + @"\" + results.Replace("/", @"\"), results);
                                    Logger("Downloading From Page Link Found : " + results);
                                }
                                else
                                {
                                    DownloadFile(RetrunNewUrl(Url).ToString() + @"\" + lineitem, Application.StartupPath + @"\" + RetrunNewUrl(Url).ToString() + @"\" + lineitem.Replace("/", @"\"), lineitem);
                                    Logger("Trying To Download Link From Page : " + lineitem);
                                }
                            }
                        }
                    }

                    /*loop trough all other files */
                    #region << Read CSS>>
                    /*start with css*/
                    for (int i = 0; i < CSSList.Count; i++)
                    {
                        string[] arrayoflines = File.ReadAllLines(CSSList[i].FileLocation);

                        foreach (String item in arrayoflines)
                        {
                            //now we find the items we require

                            string[] lines = Regex.Split(item, "\"");
                            /* for HREF */
                            foreach (var lineitem in lines)
                            {
                                if (lineitem.Contains("fonts/") || lineitem.Contains("font/"))
                                {
                                    string results = lineitem.Split('?').First();
                                    DownloadFile(RetrunNewUrl(Url).ToString() + @"\" + results, Application.StartupPath + @"\" + txtProjectName.Text.ToString() + @"\" + RetrunNewUrl(Url).ToString() + @"\" + results.Replace("/", @"\"), results.Replace("../", "/"));
                                    Logger("Downloading From CSS Found : " + results);
                                }
                                else if (lineitem.Contains("css/") || lineitem.Contains("image/") || lineitem.Contains("images/") || lineitem.Contains("fonts/") || lineitem.Contains("font/") || lineitem.Contains("js/") || lineitem.Contains("script/"))
                                {
                                    //clean the string a bit 
                                    if (lineitem.Contains("url(") && !lineitem.Contains("data:"))
                                    {
                                        int pFrom = lineitem.IndexOf("url(");
                                        if (lineitem.Contains(") "))
                                        {
                                            int pTo = lineitem.IndexOf(")");

                                            string results = lineitem.Substring(pFrom, pTo - pFrom).Replace("url(", "").Replace("../", "/");

                                            DownloadFile(RetrunNewUrl(Url).ToString() + @"\" + results, Application.StartupPath + @"\" + RetrunNewUrl(Url).ToString() + @"\" + results.Replace("/", @"\"), results);
                                            Logger("Downloading From CSS Found : " + results);

                                        }
                                        else if (lineitem.Contains(");"))
                                        {
                                            int pTo = lineitem.IndexOf(");");

                                            string results = lineitem.Substring(pFrom, pTo - pFrom).Replace("url(", "");

                                            DownloadFile(RetrunNewUrl(Url).ToString() + @"\" + results, Application.StartupPath + @"\" + RetrunNewUrl(Url).ToString() + @"\" + results.Replace("/", @"\"), results);
                                            Logger("Downloading From CSS Found : " + results);
                                        }
                                        else
                                        {

                                        }
                                    }
                                    else if (lineitem.Contains("data:") && lineitem.Contains("base64"))
                                    {
                                        string datatomine = lineitem;
                                        Logger("Downloading From CSS Found : " + lineitem);
                                    }
                                    else
                                    {
                                        DownloadFile(RetrunNewUrl(Url).ToString() + @"\" + lineitem, Application.StartupPath + @"\" + RetrunNewUrl(Url).ToString() + @"\" + lineitem.Replace("/", @"\"), lineitem);
                                        Logger("Downloading From CSS Found : " + lineitem);
                                    }
                                }
                            }
                        }
                    }
                    #endregion << Read CSS>>

                    #region << Read JS >>
                    /*next we read js*/
                    for (int i = 0; i < JSList.Count; i++)
                    {
                        string[] arrayoflines = File.ReadAllLines(JSList[i].FileLocation);

                        foreach (String item in arrayoflines)
                        {
                            //now we find the items we require

                            string[] lines = Regex.Split(item, "\"");
                            /* for HREF */
                            foreach (var lineitem in lines)
                            {
                                if (lineitem.Contains("css/") || lineitem.Contains("image/") || lineitem.Contains("images/") || lineitem.Contains("fonts/") || lineitem.Contains("font/") || lineitem.Contains("js/") || lineitem.Contains("script/"))
                                {
                                    //clean the string a bit 
                                    if (lineitem.Contains("url("))
                                    {
                                        int pFrom = lineitem.IndexOf("url(");
                                        int pTo = lineitem.LastIndexOf(");");

                                        string results = lineitem.Substring(pFrom, pTo - pFrom);

                                        DownloadFile(RetrunNewUrl(Url).ToString() + @"\" + results, Application.StartupPath + @"\" + RetrunNewUrl(Url).ToString() + @"\" + results.Replace("/", @"\"), results);
                                        Logger("Downloading From JS Found : " + results);
                                    }
                                    else
                                    {
                                        DownloadFile(RetrunNewUrl(Url).ToString() + @"\" + lineitem, Application.StartupPath + @"\" + RetrunNewUrl(Url).ToString() + @"\" + lineitem.Replace("/", @"\"), lineitem);
                                        Logger("Downloading From JS Found : " + lineitem);
                                    }
                                }
                            }
                        }

                    }
                    #endregion << Read JS >>
                }
            }
            catch (Exception ex)
            {
                ErrorStr = ErrorStr + ex + "\r\n";
            }
        }


        private void Logger(string Value)
        {
            lblDoingNow.Invoke(new Action(() => lblDoingNow.Text = Value));
        }


        #endregion << Methods >>

        #region << Events >>

        private void webpage_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            if (webBrowser1.CanGoBack) toolStripButton1.Enabled = true;
            else toolStripButton1.Enabled = false;

            if (webBrowser1.CanGoForward) toolStripButton2.Enabled = true;
            else toolStripButton2.Enabled = false;
            toolStripStatusLabel1.Text = "Done";

            //DownloadCurrentPage();
            //try
            //{
            //    string urlmain = webBrowser1.Url.ToString();
            //    string filename = urlmain.Split('/').Last();
            //    Pages.Remove(filename);
            //    /*download time*/
            //    foreach (var item in Pages)
            //    {
            //        webBrowser1.Url = new Uri(urlmain.Replace(filename, item));
            //        DownloadCurrentPage();
            //        urlmain = webBrowser1.Url.ToString();
            //        filename = urlmain.Split('/').Last();
            //        Pages.Remove(filename);
            //    }
            //}
            //catch(Exception ex)
            //{
            //    string exeptionlly = ex.Message;
            //}
        }

        private void webpage_DocumentTitleChanged(object sender, EventArgs e)
        {
            this.Text = webBrowser1.DocumentTitle.ToString();
        }
        private void webpage_StatusTextChanged(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = webBrowser1.StatusText;
        }

        private void webpage_ProgressChanged(object sender, WebBrowserProgressChangedEventArgs e)
        {
            toolStripProgressBar1.Maximum = (int)e.MaximumProgress;
            toolStripProgressBar1.Value = ((int)e.CurrentProgress < 0 || (int)e.MaximumProgress < (int)e.CurrentProgress) ? (int)e.MaximumProgress : (int)e.CurrentProgress;
        }

        private void webpage_Navigated(object sender, WebBrowserNavigatedEventArgs e)
        {
            toolStripComboBox1.Text = webBrowser1.Url.ToString();
        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            webBrowser1.Refresh();
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            webBrowser1.GoForward();
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            webBrowser1.GoBack();
        }

        private void toolStripButton5_Click(object sender, EventArgs e)
        {
            webBrowser1.GoHome();
        }

        private void toolStripButton6_Click(object sender, EventArgs e)
        {
            webBrowser1.ShowPrintPreviewDialog();
        }
        private void button1_Click(object sender, EventArgs e)
        {
            if (txtProjectName.Text == "" || txtWebsite.Text == "")
            {
                MessageBox.Show("Please fill in all fields");
                return;
            }

            /*now we browse the */
            Url = txtWebsite.Text.Trim();

            //myBrowser();

            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                Logger("Getting List OF Websites");
                #region << Get References >>
                /*get all references*/
                string data;
                using (WebClient client = new WebClient())
                {
                    System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls | System.Net.SecurityProtocolType.Tls11 | System.Net.SecurityProtocolType.Tls12;
                    client.Headers.Add("user-agent", "Only a test!");
                    string urlfrom = Url.Replace("../", "/").Replace("//", "/").Replace("\\", "/");
                if (urlfrom.Contains("https:/"))
                    urlfrom = urlfrom.Replace("https:/", "https://");
                    if (urlfrom.Contains("http:/"))
                        urlfrom = urlfrom.Replace("http:/", "http://");
                    Uri urltodownload = new Uri(urlfrom);
                    data = client.DownloadString(urltodownload);
                }

                if (data != string.Empty)
                {
                    //we found the code now we mine 
                    /*now we read line by line*/
                    string[] array = data.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                    foreach (String item in array)
                    {

                        var doc = new HtmlAgilityPack.HtmlDocument();

                        doc.LoadHtml(item);

                        var anchor = doc.DocumentNode.SelectSingleNode("//a");
                        if (anchor != null)
                        {
                            string link = anchor.Attributes["href"].Value;
                            try
                            {
                                data = string.Empty;
                                using (WebClient client = new WebClient())
                                {
                                    System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls | System.Net.SecurityProtocolType.Tls11 | System.Net.SecurityProtocolType.Tls12;
                                    client.Headers.Add("user-agent", "Only a test!");
                                    string urlfrom = RetrunNewUrl(Url).ToString().Replace("../", "/").Replace("//", "/").Replace("\\", "/") + @"\" + link;
                                    if(urlfrom.Contains("https:/"))
                                    urlfrom = urlfrom.Replace("https:/", "https://").Replace("\\", "/");
                                    if (urlfrom.Contains("http:/"))
                                        urlfrom = urlfrom.Replace("http:/", "http://").Replace("\\", "/");

                                    Uri urltodownload = new Uri(urlfrom);
                                    data = client.DownloadString(urltodownload);
                                }
                                if (!Directory.Exists(Application.StartupPath + @"\" + txtProjectName.Text.ToString()))
                                {
                                    Directory.CreateDirectory(Application.StartupPath + @"\" + txtProjectName.Text.ToString());
                                }

                                File.WriteAllText(Application.StartupPath + @"\" + txtProjectName.Text.ToString() + @"\" + link, data);
                                if (!Pages.Contains(link))
                                {
                                    Pages.Add(link);
                                    this.listBox1.Invoke(new Action(() => listBox1.Items.Add(link)));
                                }
                            }
                            catch (Exception ex)
                            {
                                string somethingwentwrong = ex.Message;
                            }



                        }
                    }
                }


                #endregion << Get References >>
                Logger("Listing Pages");
                List<string> pagesalreadyprocessed = new List<string>();

                /* no loop trough the references */
                for (int i = 0; i < Pages.Count; i++)
                {
                    
                    /*navigate to page*/
                    string urlmain = Url.ToString();
                    string filename = urlmain.Split('/').Last();
                    string urlwithoutlastpart = urlmain.Replace(filename, "");
                    Url = urlwithoutlastpart + @"\" + Pages[i].ToString().Trim();
                    if (pagesalreadyprocessed.Contains(Url))
                    {
                        return;
                    }
                    Thread.Sleep(TimeSpan.FromSeconds(5));
                    try
                    {
                        webBrowser1.Navigate(Url);
                    }
                    catch
                    {
                        //if something holds up the process we don't want to slow down the downloader
                    }
                    Logger("Downloading Page "+Pages[i]);
                    DownloadCurrentPage();
                    pagesalreadyprocessed.Add(Url);
                    this.listBox1.Invoke(new Action(() => listBox1.Items.Remove(Pages[i].ToString().Trim())));
                }

                /*after all pages are done */
                CSSList.Clear();
                pagesalreadyprocessed.Clear();
                Pages.Clear();
                JSList.Clear();
                AllOtherFiles.Clear();
                MessageBox.Show("Website successfully downloaded","Done",MessageBoxButtons.OK,MessageBoxIcon.Information);
                Logger("");
            }).Start();
        }

        #endregion << Events >>
    }
}

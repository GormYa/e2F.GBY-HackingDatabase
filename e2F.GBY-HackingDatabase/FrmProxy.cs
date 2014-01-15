using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Windows.Forms;
using HtmlAgilityPack;
using System.Threading;
using System.IO;

namespace e2F_GHDB_GUI
{
    public partial class FrmProxy : Form
    {
        readonly List<string> _downloadedProxy = new List<string>();

        public FrmProxy()
        {
            InitializeComponent();
        }

        private Thread _thrDownloadProxy;
        private void frmProxy_Load(object sender, EventArgs e)
        {
            FrmMain.ProxyList.Clear();
            //CheckForIllegalCrossThreadCalls = false;
            _downloadedProxy.Clear();

            _thrDownloadProxy = new Thread(ProxyDownloadPage);
            _thrDownloadProxy.Start();
            tmr.Enabled = true;
        }

        private void ProxyDownloadPage()
        {
            for (int pageNumber = 1; pageNumber <= 3; pageNumber++)
            {
                lblStatus.Invoke((MethodInvoker)(() => lblStatus.Text = string.Format("{0}. proxy listesi alınıyor, lütfen bekleyiniz...", pageNumber)));
                string proxyUrl = string.Format("http://proxy-list.org/english/index.php?p={0}", pageNumber);

                //Sistemde tanımlı proxy mevcut ise okunup request'e ekleniyor...
                try
                {
                    string html;
                    using (WebClient client = new WebClient())
                    {
                        client.Headers[HttpRequestHeader.UserAgent] = "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.1; WOW64; Trident/6.0)";
                        client.Headers[HttpRequestHeader.Referer] = "http://proxy-list.org/";
                        html = client.DownloadString(proxyUrl);
                    }

                    HtmlAgilityPack.HtmlDocument dokuman = new HtmlAgilityPack.HtmlDocument();
                    dokuman.LoadHtml(html);

                    HtmlNodeCollection basliklar = dokuman.DocumentNode.SelectNodes("//ul[not(@*)]//li[@class='proxy']");
                    foreach (var baslik in basliklar)
                    {
                        FrmMain.ProxyList.Add(baslik.InnerHtml);
                        _downloadedProxy.Add(baslik.InnerText);
                    }

                    lblStatus.Invoke((MethodInvoker)(() => lblStatus.Text = string.Format("{0}. proxy listesi alındı.", pageNumber)));
                }
                catch (Exception x)
                {
                    MessageBox.Show(x.Message);
                }
            }

            lblStatus.Invoke((MethodInvoker)(() => lblStatus.Text = string.Format("Proxy listesi kaydediliyor...")));

            string path = string.Format("{0}\\ProxyFile\\ProxyList-{1}-{2}-{3}.txt", Application.StartupPath, DateTime.Now.Date.Day, DateTime.Now.Date.Month, DateTime.Now.Date.Year);

            File.WriteAllText(path, string.Join(Environment.NewLine, _downloadedProxy.ToArray()));

            Invoke((MethodInvoker)(Close));
        }


        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void tmr_Tick(object sender, EventArgs e)
        {
            lblInfo.ForeColor = lblInfo.ForeColor.Equals(Color.Red) ? Color.Black : Color.Red;
        }

        private void frmProxy_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_thrDownloadProxy.ThreadState == ThreadState.Running)
            {
                _thrDownloadProxy.Abort();
            }
        }
    }
}

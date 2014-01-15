/*
 * Eğer bu kaynak kodları okuyorsan, hazıra konanlardan değilsin.
 * Hadi durma o zaman senin için açıklamalar yazdım kodlar arasına.
 * O açıklamalar yazdıklarım konusunda sana bilgiler verecek.
 * Programı daha da geliştirip sende katkıda bulunabilirsin.
 * Kaynak kodları istediğin gibi değiştirip dağıtma hakkına sahipsin.
 * Şimdi sende aynısını yap! Geliştir ve kaynak kodları open-source olarak dağıt.
 * 
 * Eyüp ÇELİK - www.eyupcelik.com.tr
 * info@eyupcelik.com.tr
 * e2F Security Software 2014
 * 
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using TreeNode = System.Windows.Forms.TreeNode;

namespace e2F_GHDB_GUI
{
    public partial class FrmMain : Form
    {
        //Sorgulanan vektörlerin sistemi yormaması için thread oluşturuldu
        Thread _scanDork;

        //Seçilen node'lar generic liste dolduruluyor. Seçilen arama sorguları generic list üzerinden aktarılıyor...
        readonly List<string> _genNode = new List<string>();
        //Bulunan proxy adresleri arama motorunu bypass etmek için generic liste dolduruluyor...
        public static readonly List<string> ProxyList = new List<string>();
        bool _googleBanned;
        public FrmMain()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            CheckForIllegalCrossThreadCalls = false;

            FrmProxy proxy = new FrmProxy();
            proxy.ShowDialog();

            Thread ghdbImport = new Thread(FileRead);
            ghdbImport.Start("GHDB");

            Thread yhdbImport = new Thread(FileRead);
            yhdbImport.Start("YHDB");

            Thread bhdbImport = new Thread(FileRead);
            bhdbImport.Start("BHDB");

            foreach (TreeNode node in treeE2F.Nodes)
            {
                node.Expand();
            }

            cmbSearchNumber.SelectedIndex = 0;

            //TreeView'de bulunan tüm Node'lar için checkbox oluşturduk
            treeE2F.CheckBoxes = true;
            cmbSearchEngine.SelectedIndex = 0;
        }

        private void FileRead(object obj)
        {
            string fileType = (string)obj;
            string filePath = string.Format("{0}\\{1}\\", Application.StartupPath, fileType);

            string[] files = Directory.GetFiles(filePath, "*.txt");
            foreach (string file in files)
            {
                string fileName = file.Replace(filePath, string.Empty);
                string nodeName = fileName.Replace(".txt", string.Empty);
                string nodeText = nodeName.Replace("_", " ");

                string[] lines = File.ReadAllLines(file);

                foreach (string line in lines)
                {
                    treeE2F.Invoke((MethodInvoker)(() => treeE2F.Nodes[fileType].Nodes[nodeName].Nodes.Add(line)));
                }

                treeE2F.Invoke((MethodInvoker)(() => treeE2F.Nodes[fileType].Nodes[nodeName].Text = string.Format("{0} ({1})", nodeText, lines.Length)));
            }

            int fileCount = treeE2F.Nodes["GHDB"].GetNodeCount(true) + treeE2F.Nodes["YHDB"].GetNodeCount(true) + treeE2F.Nodes["BHDB"].GetNodeCount(true);
            stripLblDurum.Text = string.Format("{0} adet saldırı vektörü yüklendi.", fileCount);
        }

        private void treeE2F_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            try
            {
                stripLblDurum.Text = e.Node.Text;
                treeE2F.BeginUpdate();

                //Seçilen saldırı vektörleri yakalanıyor
                foreach (TreeNode node in e.Node.Nodes)
                {
                    if (e.Node.Checked)
                    {
                        node.Checked = true;
                        _genNode.Add(node.Text);
                    }
                    else
                    {
                        node.Checked = false;
                        _genNode.Remove(node.Text);
                    }
                }

                lblSecilenNode.Text = string.Format("{0} saldırı vektörü seçildi.", _genNode.Count);

                treeE2F.EndUpdate();
            }
            catch (Exception hata)
            {
                MessageBox.Show(string.Format("İşlem sırasında bir hata oluştu.\r\nOluşan Hata: {0}", hata.Message), @"e2F GHDB GUI", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

        }
        private void lstResultData_SelectedIndexChanged(object sender, EventArgs e)
        {
            //Bulunan sitelerden herhangi biri tıklanırsa eğer
            if (lstResultData.SelectedIndex != -1)
            {
                //Tıklanan site browser'dan açılıyor
                string site = lstResultData.Items[lstResultData.SelectedIndex].ToString();
                System.Diagnostics.Process.Start(site);
            }
        }

        private void MtdGoogleDorkSearch()
        {
            try
            {
                //URL adresi tanımlandı
                string url;
                //Eğer bir Node kategorisi seçilmiş ise aşağıdaki kodlar çalışacak
                if (_genNode.Count > 0)
                {
                    //Generic listte bulunanan saldırı vektörleri for ile dönülüyor...
                    foreach (string t in _genNode)
                    {
                        //Eğer domain yazılmış ise sadece domaine ait tarama gerçekleştirilecektir
                        url = !string.IsNullOrEmpty(txtDomain.Text)
                            ? string.Format("http://ajax.googleapis.com/ajax/services/search/web?v=1.0&start=1&rsz=large&q=site:{0} {1}", txtDomain.Text, t)
                            : string.Format("http://ajax.googleapis.com/ajax/services/search/web?v=1.0&start=1&rsz=large&q={0}", t);
                        
                        //Generic listten denenen saldırı vektörü kullanıcıya bildiriliyor
                        lblSecilenNode.Text = string.Format("{0} vektör deneniyor...", t);
                        
                        //HttpWebRequest ile set edilen URL adresine istek gönderiliyor
                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                        //Gönderilen istek Googlebot'a set ediliyor...
                        
                        request.UserAgent = "Googlebot/2.1 (+http://www.google.com/bot.html)";
                        //Sistemde tanımlı proxy mevcut ise okunup request'e ekleniyor...

                        if (_googleBanned)
                        {
                            if (chkBypass.Checked)
                            {
                                stripLblDurum.Text = @"Arama motoru, dork aramayı durdurdu. Arama motoru ""Bypass"" ediliyor...";
                                stripLblDurum.ForeColor = Color.DarkRed;
                                int newRandom = new Random().Next(0, ProxyList.Count);
                                string[] ipParcala = ProxyList[newRandom].Split(':');

                                WebProxy prox = new WebProxy(ipParcala[0], Convert.ToInt32(ipParcala[1]))
                                {
                                    BypassProxyOnLocal = false
                                };
                                request.Proxy = prox;
                                stripLblDurum.Text = string.Format("Arama motoru {0}:{1} proxy adresi ile \"Bypass\" ediliyor...", ipParcala[0], ipParcala[1]);
                                stripLblDurum.ForeColor = Color.DarkRed;
                            }
                            else
                            {
                                stripLblDurum.Text = @"Arama motoru, dork aramayı durdurdu. Arama motorunun engelini aşmak için ""Bypass Engine""i seçip aramayı yeniden başlatınız...";
                                stripLblDurum.ForeColor = Color.DarkRed;
                                btnDorkSearch.Text = @"Ara";
                                break;
                            }
                        }

                        try
                        {
                            //Sunucudan dönen cevap webresponse ile yakalanıyor
                            string htmlsource;
                            using (WebResponse response = request.GetResponse())
                            {
                                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                                {
                                    htmlsource = reader.ReadToEnd();
                                    //Stream ve reader kapatılıyor
                                    response.Close();
                                    reader.Close();
                                }
                            }
                            //Stringe atanan data Durum ekranına basılıp kullanıcıya gösteriliyor...
                            txtDurum.Text = htmlsource;

                            //Eğer arama motoru dork aramayı engellerse ban true hale getirilip arama motorunun bypass edilmesi sağlanıyor...
                            _googleBanned = htmlsource == "{\"responseData\": null, \"responseDetails\": \"Suspected Terms of Service Abuse. Please see http://code.google.com/apis/errors\", \"responseStatus\": 403}";
                            //Elde edilen datanın içeriği regex ile düzeltiliyor
                            Regex exp = new Regex("(\\Wurl\\W:\\W(https?|ftp|gopher|telnet|file):?((//)|(\\\\\\\\))+[\\w\\d:#@%/;$()~_?\\+-=\\\\\\.&]*)", RegexOptions.IgnoreCase);
                            //Düzenli hale getirilmesi için regex match collection'a aktarılıyor
                            MatchCollection match = exp.Matches(txtDurum.Text);
                            //Match'e doldurulan datalar for ile dönülüyor
                            for (int a = 0; a < match.Count; a++)
                            {
                                //match'ten dönen data son bir defa replace edilip son halini alarak düzenli url ifadesine çevriliyor
                                txtDurum.Text += string.Format("{0}\r\n", match[a].ToString().Replace("\"url\":\"", ""));
                                lstResultData.Items.Add(string.Format("{0}\r\n", match[a].ToString().Replace("\"url\":\"", "")));
                            }
                            //bulunan url adedi label a aktarılıp kullanıcıya gösteriliyor
                            lblBulunanSayi.Text = Convert.ToString(match.Count);
                            //textbox'a son eklenen değere focus olunuyor...
                            txtDurum.SelectionStart = txtDurum.Text.Length;
                            txtDurum.ScrollToCaret();
                        }
                        catch (Exception x)
                        {
                            MessageBox.Show(x.Message);
                        }
                    }
                }
                //Eğer bir Node kategorisi seçilmemiş ise aşağıdaki kodlar çalışacak
                else
                {
                    //Eğer domain yazılmış ise sadece domaine ait tarama gerçekleştirilecektir
                    url = !string.IsNullOrEmpty(txtDomain.Text)
                        ? string.Format("http://ajax.googleapis.com/ajax/services/search/web?v=1.0&start=1&rsz=large&q=site:{0} {1}", txtDomain.Text, stripLblDurum.Text)
                        : string.Format("http://ajax.googleapis.com/ajax/services/search/web?v=1.0&start=1&rsz=large&q={0}", stripLblDurum.Text);

                    //Generic listten denenen saldırı vektörü kullanıcıya bildiriliyor
                    lblSecilenNode.Text = string.Format("{0} vektör deneniyor...", stripLblDurum.Text);
                    //HttpWebRequest ile set edilen URL adresine istek gönderiliyor
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                    //Gönderilen istek Googlebot'a set ediliyor...
                    request.UserAgent = "Googlebot/2.1 (+http://www.google.com/bot.html)";
                    //Sistemde tanımlı proxy mevcut ise okunup request'e ekleniyor...

                    if (_googleBanned)
                    {
                        if (chkBypass.Checked)
                        {
                            stripLblDurum.Text = @"Arama motoru, dork aramayı durdurdu. Arama motoru ""Bypass"" ediliyor...";
                            stripLblDurum.ForeColor = Color.DarkRed;
                            Random rnd = new Random();
                            int newRandom = rnd.Next(0, ProxyList.Count);
                            string[] ipParcala = ProxyList[newRandom].Split(':');

                            WebProxy prox = new WebProxy(ipParcala[0], Convert.ToInt32(ipParcala[1]))
                            {
                                BypassProxyOnLocal = false
                            };
                            request.Proxy = prox;
                            stripLblDurum.Text = string.Format("Arama motoru {0}:{1} proxy adresi ile \"Bypass\" ediliyor...", ipParcala[0], ipParcala[1]);
                            stripLblDurum.ForeColor = Color.DarkRed;
                        }
                        else
                        {
                            stripLblDurum.Text = @"Arama motoru, dork aramayı durdurdu. Arama motorunun engelini aşmak için ""Bypass Engine""i seçip aramayı yeniden başlatınız...";
                            stripLblDurum.ForeColor = Color.DarkRed;
                            btnDorkSearch.Text = @"Ara";
                        }
                    }
                    
                    try
                    {
                        //Sunucudan dönen cevap webresponse ile yakalanıyor
                        string htmlsource;
                        using (WebResponse response = request.GetResponse())
                        {
                            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                            {
                                htmlsource = reader.ReadToEnd();
                                //Stream ve reader kapatılıyor
                                response.Close();
                                reader.Close();
                            }
                        }
                        //Stringe atanan data Durum ekranına basılıp kullanıcıya gösteriliyor...
                        txtDurum.Text = htmlsource;
                        //Eğer arama motoru dork aramayı engellerse ban true hale getirilip arama motorunun bypass edilmesi sağlanıyor...
                        _googleBanned = htmlsource == "{\"responseData\": null, \"responseDetails\": \"Suspected Terms of Service Abuse. Please see http://code.google.com/apis/errors\", \"responseStatus\": 403}";
                        //Elde edilen datanın içeriği regex ile düzeltiliyor
                        Regex exp = new Regex("(\\Wurl\\W:\\W(https?|ftp|gopher|telnet|file):?((//)|(\\\\\\\\))+[\\w\\d:#@%/;$()~_?\\+-=\\\\\\.&]*)", RegexOptions.IgnoreCase);
                        //Düzenli hale getirilmesi için regex match collection'a aktarılıyor
                        MatchCollection match = exp.Matches(txtDurum.Text);
                        //Match'e doldurulan datalar for ile dönülüyor
                        for (int a = 0; a < match.Count; a++)
                        {
                            //match'ten dönen data son bir defa replace edilip son halini alarak düzenli url ifadesine çevriliyor
                            txtDurum.Text += string.Format("{0}\r\n", match[a].ToString().Replace("\"url\":\"", string.Empty));
                            lstResultData.Items.Add(string.Format("{0}\r\n", match[a].ToString().Replace("\"url\":\"", string.Empty)));
                        }
                        //bulunan url adedi label a aktarılıp kullanıcıya gösteriliyor
                        lblBulunanSayi.Text = Convert.ToString(match.Count);
                        //textbox'a son eklenen değere focus olunuyor...
                        txtDurum.SelectionStart = txtDurum.Text.Length;
                        txtDurum.ScrollToCaret();
                    }
                    catch (Exception x)
                    {
                        MessageBox.Show(x.Message);
                    }

                }
                statusStrip.Text = @"Arama işlemi tamamlandı...";
                btnDorkSearch.Text = @"Ara";
            }
            catch (Exception hata)
            {
                MessageBox.Show(string.Format("İşlem sırasında bir hata meydana geldi.\r\nOluşan Hata: {0}", hata.Message), @"e2F Google Hacking Database GUI", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void MtdBingDorkSearch()
        {
            try
            {
                //URL adresi tanımlandı
                string url;
                //Eğer bir Node kategorisi seçilmiş ise aşağıdaki kodlar çalışacak
                if (_genNode.Count > 0)
                {
                    //Generic listte bulunanan saldırı vektörleri for ile dönülüyor...
                    foreach (string t in _genNode)
                    {
                        //Eğer domain yazılmış ise sadece domaine ait tarama gerçekleştirilecektir
                        url = !string.IsNullOrEmpty(txtDomain.Text) ? string.Format("http://www.bing.com/search?q=site:{0} {1}&first=1&FORM=PERE", txtDomain.Text, t) : string.Format("http://www.bing.com/search?q={0}&first=1&FORM=PERE", t);
                        //Generic listten denenen saldırı vektörü kullanıcıya bildiriliyor
                        lblSecilenNode.Text = string.Format("{0} vektör deneniyor...", t);
                        //HttpWebRequest ile set edilen URL adresine istek gönderiliyor
                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                        //Gönderilen istek Googlebot'a set ediliyor...
                        request.UserAgent = "Googlebot/2.1 (+http://www.google.com/bot.html)";
                        //Sistemde tanımlı proxy mevcut ise okunup request'e ekleniyor...

                        if (lstResultData.Items.Count > 15)
                        {
                            if (chkBypass.Checked)
                            {
                                stripLblDurum.Text = @"Arama motoru standart araması ""Bypass"" ediliyor...";
                                stripLblDurum.ForeColor = Color.DarkRed;
                                Random rnd = new Random();
                                int newRandom = rnd.Next(0, ProxyList.Count);
                                string[] ipParcala = ProxyList[newRandom].Split(':');

                                WebProxy prox = new WebProxy(ipParcala[0], Convert.ToInt32(ipParcala[1]))
                                {
                                    BypassProxyOnLocal = false
                                };
                                request.Proxy = prox;
                                stripLblDurum.Text = string.Format("Arama motoru {0}:{1} proxy adresi ile \"Bypass\" ediliyor...", ipParcala[0], ipParcala[1]);
                                stripLblDurum.ForeColor = Color.DarkRed;
                            }
                            else
                            {
                                stripLblDurum.Text = @"Arama motoru, dork aramayı durdurdu. Arama motorunun engelini aşmak için ""Bypass Engine""i seçip aramayı yeniden başlatınız...";
                                stripLblDurum.ForeColor = Color.DarkRed;
                                if (_scanDork.ThreadState == ThreadState.Running)
                                {
                                    _scanDork.Abort();
                                    btnDorkSearch.Text = @"Ara";
                                    break;
                                }

                                btnDorkSearch.Text = @"Ara";
                                break;
                            }
                        }

                        try
                        {
                            //Sunucudan dönen cevap webresponse ile yakalanıyor
                            string htmlsource;
                            using (WebResponse response = request.GetResponse())
                            {
                                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                                {
                                    htmlsource = reader.ReadToEnd();
                                    //Stream ve reader kapatılıyor
                                    response.Close();
                                    reader.Close();
                                }
                            }
                            //Stringe atanan data Durum ekranına basılıp kullanıcıya gösteriliyor...
                            txtDurum.Text = htmlsource;
                            //Elde edilen datanın içeriği regex ile düzeltiliyor
                            Regex exp = new Regex("(href=\\W(https?|ftp|gopher|telnet|file):?((//)|(\\\\\\\\))+[\\w\\d:#@%/;$()~_?\\+-=\\\\\\.&]*)", RegexOptions.IgnoreCase);
                            //Düzenli hale getirilmesi için regex match collection'a aktarılıyor
                            MatchCollection match = exp.Matches(txtDurum.Text);
                            //Match'e doldurulan datalar for ile dönülüyor
                            for (int a = 0; a < match.Count; a++)
                            {
                                //match'ten dönen data son bir defa replace edilip son halini alarak düzenli url ifadesine çevriliyor
                                txtDurum.Text += string.Format("{0}\r\n", match[a].ToString().Replace(")\" href=\"", string.Empty).Replace("href=\"", string.Empty));
                                lstResultData.Items.Add(string.Format("{0}\r\n", match[a].ToString().Replace(")\" href=\"", string.Empty).Replace("href=\"", string.Empty)));
                            }
                            //bulunan url adedi label a aktarılıp kullanıcıya gösteriliyor
                            lblBulunanSayi.Text = Convert.ToString(match.Count);
                            //textbox'a son eklenen değere focus olunuyor...
                            txtDurum.SelectionStart = txtDurum.Text.Length;
                            txtDurum.ScrollToCaret();
                        }
                        catch (Exception x)
                        {
                            MessageBox.Show(x.Message);
                        }
                    }
                }
                //Eğer bir Node kategorisi seçilmemiş ise aşağıdaki kodlar çalışacak
                else
                {
                    //Eğer domain yazılmış ise sadece domaine ait tarama gerçekleştirilecektir
                    url = !string.IsNullOrEmpty(txtDomain.Text) ? string.Format("http://www.bing.com/search?q=site:{0} {1}&first=1&FORM=PERE", txtDomain.Text, stripLblDurum.Text) : string.Format("http://www.bing.com/search?q={0}&first=1&FORM=PERE", stripLblDurum.Text);
                    //Generic listten denenen saldırı vektörü kullanıcıya bildiriliyor
                    lblSecilenNode.Text = string.Format("{0} vektör deneniyor...", stripLblDurum.Text);
                    //HttpWebRequest ile set edilen URL adresine istek gönderiliyor
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                    //Gönderilen istek Googlebot'a set ediliyor...
                    request.UserAgent = "Googlebot/2.1 (+http://www.google.com/bot.html)";
                    //Sistemde tanımlı proxy mevcut ise okunup request'e ekleniyor...

                    if (lstResultData.Items.Count > 15)
                    {
                        if (chkBypass.Checked)
                        {
                            stripLblDurum.Text = @"Arama motoru standart araması ""Bypass"" ediliyor...";
                            stripLblDurum.ForeColor = Color.DarkRed;
                            Random rnd = new Random();
                            int newRandom = rnd.Next(0, ProxyList.Count);
                            string[] ipParcala = ProxyList[newRandom].Split(':');

                            WebProxy prox = new WebProxy(ipParcala[0], Convert.ToInt32(ipParcala[1]))
                            {
                                BypassProxyOnLocal = false
                            };
                            request.Proxy = prox;
                            stripLblDurum.Text = string.Format("Arama motoru {0}:{1} proxy adresi ile \"Bypass\" ediliyor...", ipParcala[0], ipParcala[1]);
                            stripLblDurum.ForeColor = Color.DarkRed;
                        }
                        else
                        {
                            stripLblDurum.Text = @"Arama motoru, dork aramayı durdurdu. Arama motorunun engelini aşmak için ""Bypass Engine""i seçip aramayı yeniden başlatınız...";
                            stripLblDurum.ForeColor = Color.DarkRed;
                            if (_scanDork.ThreadState == ThreadState.Running)
                            {
                                _scanDork.Abort();
                                btnDorkSearch.Text = @"Ara";
                            }
                            else
                            {
                                btnDorkSearch.Text = @"Ara";
                            }
                        }
                    }
                    try
                    {
                        //Sunucudan dönen cevap webresponse ile yakalanıyor
                        string htmlsource;
                        using (WebResponse response = request.GetResponse())
                        {
                            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                            {
                                htmlsource = reader.ReadToEnd();
                                //Stream ve reader kapatılıyor
                                response.Close();
                                reader.Close();
                            }
                        }
                        //Stringe atanan data Durum ekranına basılıp kullanıcıya gösteriliyor...
                        txtDurum.Text = htmlsource;
                        //Elde edilen datanın içeriği regex ile düzeltiliyor
                        Regex exp = new Regex("(href=\\W(https?|ftp|gopher|telnet|file):?((//)|(\\\\\\\\))+[\\w\\d:#@%/;$()~_?\\+-=\\\\\\.&]*)", RegexOptions.IgnoreCase);
                        //Düzenli hale getirilmesi için regex match collection'a aktarılıyor
                        MatchCollection match = exp.Matches(txtDurum.Text);
                        //Match'e doldurulan datalar for ile dönülüyor
                        for (int a = 0; a < match.Count; a++)
                        {
                            //match'ten dönen data son bir defa replace edilip son halini alarak düzenli url ifadesine çevriliyor
                            txtDurum.Text += string.Format("{0}\r\n", match[a].ToString().Replace(")\" href=\"", "").Replace("href=\"", ""));
                            lstResultData.Items.Add(string.Format("{0}\r\n", match[a].ToString().Replace(")\" href=\"", "").Replace("href=\"", "")));
                        }
                        //bulunan url adedi label a aktarılıp kullanıcıya gösteriliyor
                        lblBulunanSayi.Text = Convert.ToString(match.Count);
                        //textbox'a son eklenen değere focus olunuyor...
                        txtDurum.SelectionStart = txtDurum.Text.Length;
                        txtDurum.ScrollToCaret();
                    }
                    catch (Exception x)
                    {
                        MessageBox.Show(x.Message);
                    }

                }

                for (int k = 0; k < lstResultData.Items.Count; k++)
                {
                    stripLblDurum.Text = @"Sonuçlar düzeltiliyor, hatalı kayıtlar listeden temizleniyor...";
                    stripLblDurum.ForeColor = Color.DarkRed;
                    string[] delResult = { "http://go.microsoft.com/fwlink/?LinkId=248686", "http://g.msn.com/1ewbingwin8/settingsTOUtr-TR", "http://onlinehelp.microsoft.com/tr-TR/bing/ff808535.aspx" };
                    int index1 = lstResultData.FindString(delResult[0], 0);
                    int index2 = lstResultData.FindString(delResult[1], 0);
                    int index3 = lstResultData.FindString(delResult[2], 0);
                    if (index1 != -1)
                    {
                        lstResultData.Items.RemoveAt(index1);
                    }
                    if (index2 != -1)
                    {
                        lstResultData.Items.RemoveAt(index2);
                    }
                    if (index3 != -1)
                    {
                        lstResultData.Items.RemoveAt(index3);
                    }
                }
                lblBulunanSayi.Text = Convert.ToString(lstResultData.Items.Count);
                stripLblDurum.Text = @"Arama işlemi tamamlandı...";
                btnDorkSearch.Text = @"Ara";
            }
            catch (Exception hata)
            {

                MessageBox.Show(string.Format("İşlem sırasında bir hata meydana geldi.\r\nOluşan Hata: {0}", hata.Message), @"e2F Google Hacking Database GUI", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void MtdYandexDorkSearch()
        {
            try
            {
                //URL adresi tanımlandı
                string url;
                //Eğer bir Node kategorisi seçilmiş ise aşağıdaki kodlar çalışacak
                if (_genNode.Count > 0)
                {
                    //Generic listte bulunanan saldırı vektörleri for ile dönülüyor...
                    foreach (string t in _genNode)
                    {
                        //Eğer domain yazılmış ise sadece domaine ait tarama gerçekleştirilecektir
                        url = !string.IsNullOrEmpty(txtDomain.Text) ? string.Format("http://www.yandex.com/yandsearch?text=site:{0} {1}&lr=87", txtDomain.Text, t) : string.Format("http://www.yandex.com/yandsearch?text={0}&lr=87", t);
                        //Generic listten denenen saldırı vektörü kullanıcıya bildiriliyor
                        lblSecilenNode.Text = string.Format("{0} vektör deneniyor...", t);
                        //HttpWebRequest ile set edilen URL adresine istek gönderiliyor
                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                        //Gönderilen istek Googlebot'a set ediliyor...
                        request.UserAgent = "Googlebot/2.1 (+http://www.google.com/bot.html)";
                        //Sistemde tanımlı proxy mevcut ise okunup request'e ekleniyor...

                        if (lstResultData.Items.Count > 15)
                        {
                            if (chkBypass.Checked)
                            {
                                stripLblDurum.Text = @"Arama motoru standart araması ""Bypass"" ediliyor...";
                                stripLblDurum.ForeColor = Color.DarkRed;
                                Random rnd = new Random();
                                int newRandom = rnd.Next(0, ProxyList.Count);
                                string[] ipParcala = ProxyList[newRandom].Split(':');

                                WebProxy prox = new WebProxy(ipParcala[0], Convert.ToInt32(ipParcala[1]))
                                {
                                    BypassProxyOnLocal = false
                                };
                                request.Proxy = prox;
                                stripLblDurum.Text = string.Format("Arama motoru {0}:{1} proxy adresi ile \"Bypass\" ediliyor...", ipParcala[0], ipParcala[1]);
                                stripLblDurum.ForeColor = Color.DarkRed;
                            }
                            else
                            {
                                stripLblDurum.Text = @"Arama motoru, dork aramayı durdurdu. Arama motorunun engelini aşmak için ""Bypass Engine""i seçip aramayı yeniden başlatınız...";
                                stripLblDurum.ForeColor = Color.DarkRed;
                                if (_scanDork.ThreadState == ThreadState.Running)
                                {
                                    _scanDork.Abort();
                                    btnDorkSearch.Text = @"Ara";
                                    break;
                                }

                                btnDorkSearch.Text = @"Ara";
                                break;
                            }
                        }

                        try
                        {
                            //Sunucudan dönen cevap webresponse ile yakalanıyor
                            string htmlsource;
                            using (WebResponse response = request.GetResponse())
                            {
                                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                                {
                                    htmlsource = reader.ReadToEnd();
                                    //Stream ve reader kapatılıyor
                                    response.Close();
                                    reader.Close();
                                }
                            }
                            //Stringe atanan data Durum ekranına basılıp kullanıcıya gösteriliyor...
                            txtDurum.Text = htmlsource;
                            //Elde edilen datanın içeriği regex ile düzeltiliyor
                            Regex exp = new Regex("(href=\\W(https?|ftp|gopher|telnet|file):?((//)|(\\\\\\\\))+[\\w\\d:#@%/;$()~_?\\+-=\\\\\\.&]*)", RegexOptions.IgnoreCase);
                            //Düzenli hale getirilmesi için regex match collection'a aktarılıyor
                            MatchCollection match = exp.Matches(txtDurum.Text);
                            //Match'e doldurulan datalar for ile dönülüyor
                            for (int a = 0; a < match.Count; a++)
                            {
                                //match'ten dönen data son bir defa replace edilip son halini alarak düzenli url ifadesine çevriliyor
                                txtDurum.Text += string.Format("{0}\r\n", match[a].ToString().Replace(")\" href=\"", "").Replace("href=\"", ""));
                                lstResultData.Items.Add(string.Format("{0}\r\n", match[a].ToString().Replace(")\" href=\"", "").Replace("href=\"", "")));
                            }
                            //bulunan url adedi label a aktarılıp kullanıcıya gösteriliyor
                            lblBulunanSayi.Text = Convert.ToString(match.Count);
                            //textbox'a son eklenen değere focus olunuyor...
                            txtDurum.SelectionStart = txtDurum.Text.Length;
                            txtDurum.ScrollToCaret();
                        }
                        catch (Exception x)
                        {
                            MessageBox.Show(x.Message);
                        }
                    }
                }
                //Eğer bir Node kategorisi seçilmemiş ise aşağıdaki kodlar çalışacak
                else
                {
                    //Eğer domain yazılmış ise sadece domaine ait tarama gerçekleştirilecektir
                    url = !string.IsNullOrEmpty(txtDomain.Text) ? string.Format("http://www.yandex.com/yandsearch?text=site:{0} {1}&first=1&FORM=PERE", txtDomain.Text, stripLblDurum.Text) : string.Format("http://www.yandex.com/yandsearch?text={0}&first=1&FORM=PERE", stripLblDurum.Text);
                    //Generic listten denenen saldırı vektörü kullanıcıya bildiriliyor
                    lblSecilenNode.Text = string.Format("{0} vektör deneniyor...", stripLblDurum.Text);
                    //HttpWebRequest ile set edilen URL adresine istek gönderiliyor
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                    //Gönderilen istek Googlebot'a set ediliyor...
                    request.UserAgent = "Googlebot/2.1 (+http://www.google.com/bot.html)";
                    //Sistemde tanımlı proxy mevcut ise okunup request'e ekleniyor...

                    if (lstResultData.Items.Count > 15)
                    {
                        if (chkBypass.Checked)
                        {
                            stripLblDurum.Text = @"Arama motoru standart araması ""Bypass"" ediliyor...";
                            stripLblDurum.ForeColor = Color.DarkRed;
                            Random rnd = new Random();
                            int newRandom = rnd.Next(0, ProxyList.Count);
                            string[] ipParcala = ProxyList[newRandom].Split(':');

                            WebProxy prox = new WebProxy(ipParcala[0], Convert.ToInt32(ipParcala[1]))
                            {
                                BypassProxyOnLocal = false
                            };
                            request.Proxy = prox;
                            stripLblDurum.Text = string.Format("Arama motoru {0}:{1} proxy adresi ile \"Bypass\" ediliyor...", ipParcala[0], ipParcala[1]);
                            stripLblDurum.ForeColor = Color.DarkRed;
                        }
                        else
                        {
                            stripLblDurum.Text = @"Arama motoru, dork aramayı durdurdu. Arama motorunun engelini aşmak için ""Bypass Engine""i seçip aramayı yeniden başlatınız...";
                            stripLblDurum.ForeColor = Color.DarkRed;
                            if (_scanDork.ThreadState == ThreadState.Running)
                            {
                                _scanDork.Abort();
                            }
                            btnDorkSearch.Text = @"Ara";
                        }
                    }

                    try
                    {
                        //Sunucudan dönen cevap webresponse ile yakalanıyor
                        WebResponse response = request.GetResponse();
                        //Cevap stream'e dolduruluyor
                        StreamReader reader = new StreamReader(response.GetResponseStream());
                        //Stream ile alınan data içeriği işlem yapmak üzere stringe atanıyor
                        string htmlsource = reader.ReadToEnd();
                        //Stream ve reader kapatılıyor
                        response.Close();
                        reader.Close();
                        //Stringe atanan data Durum ekranına basılıp kullanıcıya gösteriliyor...
                        txtDurum.Text = htmlsource;
                        //Elde edilen datanın içeriği regex ile düzeltiliyor
                        Regex exp = new Regex("(href=\\W(https?|ftp|gopher|telnet|file):?((//)|(\\\\\\\\))+[\\w\\d:#@%/;$()~_?\\+-=\\\\\\.&]*)", RegexOptions.IgnoreCase);
                        //Düzenli hale getirilmesi için regex match collection'a aktarılıyor
                        MatchCollection match = exp.Matches(txtDurum.Text);
                        //Match'e doldurulan datalar for ile dönülüyor
                        for (int a = 0; a < match.Count; a++)
                        {
                            //match'ten dönen data son bir defa replace edilip son halini alarak düzenli url ifadesine çevriliyor
                            txtDurum.Text += string.Format("{0}\r\n", match[a].ToString().Replace(")\" href=\"", "").Replace("href=\"", ""));
                            lstResultData.Items.Add(string.Format("{0}\r\n", match[a].ToString().Replace(")\" href=\"", "").Replace("href=\"", "")));
                        }
                        //bulunan url adedi label a aktarılıp kullanıcıya gösteriliyor
                        lblBulunanSayi.Text = Convert.ToString(match.Count);
                        //textbox'a son eklenen değere focus olunuyor...
                        txtDurum.SelectionStart = txtDurum.Text.Length;
                        txtDurum.ScrollToCaret();
                    }
                    catch (Exception x)
                    {
                        MessageBox.Show(x.Message);
                    }

                }

                for (int k = 0; k < lstResultData.Items.Count; k++)
                {
                    stripLblDurum.Text = @"Sonuçlar düzeltiliyor, hatalı kayıtlar listeden temizleniyor...";
                    stripLblDurum.ForeColor = Color.DarkRed;
                    if (lstResultData.Items.Count > 8)
                    {
                        for (int f = 0; f < lstResultData.Items.Count; f++)
                        {
                            lstResultData.Items.RemoveAt(f);
                        }
                    }
                }
                lblBulunanSayi.Text = Convert.ToString(lstResultData.Items.Count);
                stripLblDurum.Text = @"Arama işlemi tamamlandı...";
                btnDorkSearch.Text = @"Ara";
            }
            catch (Exception hata)
            {

                MessageBox.Show(string.Format("İşlem sırasında bir hata meydana geldi.\r\nOluşan Hata: {0}", hata.Message), @"e2F Google Hacking Database GUI", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if ("Dur".Equals(btnDorkSearch.Text))
            {
                //Thread durumu sorgulanıyor, eğer thread çalışıyorsa durdurulacak
                if (_scanDork.ThreadState == ThreadState.Running)
                {
                    //Thread çalışıyorsa önce suspend konuma alınıyor. Bir anda thread'i kesmek programın hataya düşmesine neden olabilir.
                    //_scanDork.Suspend();
                    //Thread suspend konumdan durdurulma konumuna set ediliyor...
                    _scanDork.Abort();
                    btnDorkSearch.Text = @"Ara";
                    stripLblDurum.Text = @"Arama isteği durduruldu...";
                    Application.Exit();
                }

            }
        }
        private void btnDorkSearch_Click(object sender, EventArgs e)
        {
            try
            {
                if (cmbSearchEngine.SelectedIndex == 0)
                {
                    if (btnDorkSearch.Text == @"Ara")
                    {
                        //Oluşturulan thread çağırılıp, GHDB sorgu metodu thread e atanıyor ve çalıştırılıyor...
                        _scanDork = new Thread(MtdGoogleDorkSearch);
                        _scanDork.Start();
                        btnDorkSearch.Text = @"Dur";
                        stripLblDurum.Text = @"Arama işlemi başladı...";
                    }
                    else
                    {
                        //Thread durumu sorgulanıyor, eğer thread çalışıyorsa durdurulacak
                        if (_scanDork.ThreadState == ThreadState.Running)
                        {
                            //Thread durdurulma konumuna set ediliyor...
                            _scanDork.Abort();
                            btnDorkSearch.Text = @"Ara";
                            stripLblDurum.Text = @"Arama isteği durduruldu...";
                        }

                    }
                }
                else if (cmbSearchEngine.SelectedIndex == 1)
                {
                    if (btnDorkSearch.Text == @"Ara")
                    {
                        //Oluşturulan thread çağırılıp, GHDB sorgu metodu thread e atanıyor ve çalıştırılıyor...
                        _scanDork = new Thread(MtdBingDorkSearch);
                        _scanDork.Start();
                        btnDorkSearch.Text = @"Dur";
                        stripLblDurum.Text = @"Arama işlemi başladı...";
                    }
                    else
                    {
                        //Thread durumu sorgulanıyor, eğer thread çalışıyorsa durdurulacak
                        if (_scanDork.ThreadState == ThreadState.Running)
                        {
                            //Thread durdurulma konumuna set ediliyor...
                            _scanDork.Abort();
                            btnDorkSearch.Text = @"Ara";
                            stripLblDurum.Text = @"Arama isteği durduruldu...";
                        }

                    }
                }
                else
                {
                    if (btnDorkSearch.Text == @"Ara")
                    {
                        //Oluşturulan thread çağırılıp, GHDB sorgu metodu thread e atanıyor ve çalıştırılıyor...
                        _scanDork = new Thread(MtdYandexDorkSearch);
                        _scanDork.Start();
                        btnDorkSearch.Text = @"Dur";
                        stripLblDurum.Text = @"Arama işlemi başladı...";
                    }
                    else
                    {
                        //Thread durumu sorgulanıyor, eğer thread çalışıyorsa durdurulacak
                        if (_scanDork.ThreadState == ThreadState.Running)
                        {
                            //Thread durdurulma konumuna set ediliyor...
                            _scanDork.Abort();
                            btnDorkSearch.Text = @"Ara";
                            stripLblDurum.Text = @"Arama isteği durduruldu...";
                        }
                    }
                }

            }
            catch (Exception hata)
            {
                MessageBox.Show(string.Format("İşlem sırasında bir hata oluştu.\r\nOluşan Hata: {0}", hata.Message), @"e2F GHDB GUI", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        private void btnSingleDorkSearch_Click(object sender, EventArgs e)
        {
            if (cmbSearchEngine.SelectedIndex == 0)
            {
                #region

                //Eğer domain yazılmış ise sadece domaine ait tarama gerçekleştirilecektir
                string url = !string.IsNullOrEmpty(txtDomain.Text) ? string.Format("http://ajax.googleapis.com/ajax/services/search/web?v=1.0&start=1&rsz=large&q=site:{0} {1}", txtDomain.Text, txtSingleDork.Text) : string.Format("http://ajax.googleapis.com/ajax/services/search/web?v=1.0&start=1&rsz=large&q={0}", txtSingleDork.Text);
                //Generic listten denenen saldırı vektörü kullanıcıya bildiriliyor
                lblSecilenNode.Text = string.Format("{0} vektörü deneniyor...", txtSingleDork.Text);
                //HttpWebRequest ile set edilen URL adresine istek gönderiliyor
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                //Gönderilen istek Googlebot'a set ediliyor...
                request.UserAgent = "Googlebot/2.1 (+http://www.google.com/bot.html)";
                //Sistemde tanımlı proxy mevcut ise okunup request'e ekleniyor...

                if (_googleBanned)
                {
                    if (chkBypass.Checked)
                    {
                        stripLblDurum.Text = @"Arama motoru, dork aramayı durdurdu. Arama motoru ""Bypass"" ediliyor...";
                        stripLblDurum.ForeColor = Color.DarkRed;
                        Random rnd = new Random();
                        int newRandom = rnd.Next(0, ProxyList.Count);
                        string[] ipParcala = ProxyList[newRandom].Split(':');

                        WebProxy prox = new WebProxy(ipParcala[0], Convert.ToInt32(ipParcala[1]))
                        {
                            BypassProxyOnLocal = false
                        };
                        request.Proxy = prox;
                        stripLblDurum.Text = string.Format("Arama motoru {0}:{1} proxy adresi ile \"Bypass\" ediliyor...", ipParcala[0], ipParcala[1]);
                        stripLblDurum.ForeColor = Color.DarkRed;
                    }
                    else
                    {
                        stripLblDurum.Text = @"Arama motoru, dork aramayı durdurdu. Arama motorunun engelini aşmak için ""Bypass Engine""i seçip aramayı yeniden başlatınız...";
                        stripLblDurum.ForeColor = Color.DarkRed;
                        btnDorkSearch.Text = @"Ara";
                    }
                }
                try
                {
                    //Sunucudan dönen cevap webresponse ile yakalanıyor
                    string htmlsource;
                    using (WebResponse response = request.GetResponse())
                    {
                        using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                        {
                            htmlsource = reader.ReadToEnd();
                            //Stream ve reader kapatılıyor
                            response.Close();
                            reader.Close();
                        }
                    }
                    //Stringe atanan data Durum ekranına basılıp kullanıcıya gösteriliyor...
                    txtDurum.Text = htmlsource;
                    //Eğer arama motoru dork aramayı engellerse ban true hale getirilip arama motorunun bypass edilmesi sağlanıyor...
                    _googleBanned = htmlsource == "{\"responseData\": null, \"responseDetails\": \"Suspected Terms of Service Abuse. Please see http://code.google.com/apis/errors\", \"responseStatus\": 403}";
                    //Elde edilen datanın içeriği regex ile düzeltiliyor
                    Regex exp = new Regex("(\\Wurl\\W:\\W(https?|ftp|gopher|telnet|file):?((//)|(\\\\\\\\))+[\\w\\d:#@%/;$()~_?\\+-=\\\\\\.&]*)", RegexOptions.IgnoreCase);
                    //Düzenli hale getirilmesi için regex match collection'a aktarılıyor
                    MatchCollection match = exp.Matches(txtDurum.Text);
                    //Match'e doldurulan datalar for ile dönülüyor
                    for (int a = 0; a < match.Count; a++)
                    {
                        //match'ten dönen data son bir defa replace edilip son halini alarak düzenli url ifadesine çevriliyor
                        txtDurum.Text += string.Format("{0}\r\n", match[a].ToString().Replace("\"url\":\"", ""));
                        lstResultData.Items.Add(string.Format("{0}\r\n", match[a].ToString().Replace("\"url\":\"", "")));
                    }
                    //bulunan url adedi label a aktarılıp kullanıcıya gösteriliyor
                    lblBulunanSayi.Text = Convert.ToString(match.Count);
                    //textbox'a son eklenen değere focus olunuyor...
                    txtDurum.SelectionStart = txtDurum.Text.Length;
                    txtDurum.ScrollToCaret();
                }
                catch (Exception x)
                {
                    MessageBox.Show(x.Message);
                }
                #endregion
            }
            else if (cmbSearchEngine.SelectedIndex == 1)
            {
                #region

                string url = !string.IsNullOrEmpty(txtDomain.Text) ? string.Format("http://www.bing.com/search?q=site:{0} {1}&first=1&FORM=PERE", txtDomain.Text, txtSingleDork.Text) : string.Format("http://www.bing.com/search?q={0}&first=1&FORM=PERE", txtSingleDork.Text);

                //Generic listten denenen saldırı vektörü kullanıcıya bildiriliyor
                lblSecilenNode.Text = string.Format("{0} vektör deneniyor...", txtSingleDork.Text);
                //HttpWebRequest ile set edilen URL adresine istek gönderiliyor
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                //Gönderilen istek Googlebot'a set ediliyor...
                request.UserAgent = "Googlebot/2.1 (+http://www.google.com/bot.html)";
                //Sistemde tanımlı proxy mevcut ise okunup request'e ekleniyor...

                if (lstResultData.Items.Count > 15)
                {
                    if (chkBypass.Checked)
                    {
                        stripLblDurum.Text = @"Arama motoru standart araması ""Bypass"" ediliyor...";
                        stripLblDurum.ForeColor = Color.DarkRed;
                        Random rnd = new Random();
                        int newRandom = rnd.Next(0, ProxyList.Count);
                        string[] ipParcala = ProxyList[newRandom].Split(':');

                        WebProxy prox = new WebProxy(ipParcala[0], Convert.ToInt32(ipParcala[1]))
                        {
                            BypassProxyOnLocal = false
                        };

                        request.Proxy = prox;
                        stripLblDurum.Text = string.Format("Arama motoru {0}:{1} proxy adresi ile \"Bypass\" ediliyor...", ipParcala[0], ipParcala[1]);
                        stripLblDurum.ForeColor = Color.DarkRed;
                    }
                    else
                    {
                        stripLblDurum.Text = @"Arama motoru, dork aramayı durdurdu. Arama motorunun engelini aşmak için ""Bypass Engine""i seçip aramayı yeniden başlatınız...";
                        stripLblDurum.ForeColor = Color.DarkRed;
                        if (_scanDork.ThreadState == ThreadState.Running)
                        {
                            _scanDork.Abort();
                        }

                        btnDorkSearch.Text = @"Ara";
                    }
                }
                try
                {
                    //Sunucudan dönen cevap webresponse ile yakalanıyor
                    string htmlsource;
                    using (WebResponse response = request.GetResponse())
                    {
                        using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                        {
                            htmlsource = reader.ReadToEnd();
                            //Stream ve reader kapatılıyor
                            response.Close();
                            reader.Close();
                        }
                    }
                    //Stringe atanan data Durum ekranına basılıp kullanıcıya gösteriliyor...
                    txtDurum.Text = htmlsource;
                    //Elde edilen datanın içeriği regex ile düzeltiliyor
                    Regex exp = new Regex("(href=\\W(https?|ftp|gopher|telnet|file):?((//)|(\\\\\\\\))+[\\w\\d:#@%/;$()~_?\\+-=\\\\\\.&]*)", RegexOptions.IgnoreCase);
                    //Düzenli hale getirilmesi için regex match collection'a aktarılıyor
                    MatchCollection match = exp.Matches(txtDurum.Text);
                    //Match'e doldurulan datalar for ile dönülüyor
                    for (int a = 0; a < match.Count; a++)
                    {
                        //match'ten dönen data son bir defa replace edilip son halini alarak düzenli url ifadesine çevriliyor
                        txtDurum.Text += string.Format("{0}\r\n", match[a].ToString().Replace(")\" href=\"", "").Replace("href=\"", ""));
                        lstResultData.Items.Add(string.Format("{0}\r\n", match[a].ToString().Replace(")\" href=\"", "").Replace("href=\"", "")));
                    }
                    //bulunan url adedi label a aktarılıp kullanıcıya gösteriliyor
                    lblBulunanSayi.Text = Convert.ToString(match.Count);
                    //textbox'a son eklenen değere focus olunuyor...
                    txtDurum.SelectionStart = txtDurum.Text.Length;
                    txtDurum.ScrollToCaret();
                }
                catch (Exception x)
                {
                    MessageBox.Show(x.Message);
                }
                #endregion
            }
        }

        private void menuSettingsProxyServer_Click(object sender, EventArgs e)
        {
            FrmProxy proxy = new FrmProxy();
            proxy.ShowDialog();
        }
        private void menuFileSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (lstResultData.Items.Count > 0)
                {
                    File.WriteAllText(string.Format("{0}\\e2F_GHDB_Arama_Sonuclari.txt", Application.StartupPath), string.Join(string.Empty, lstResultData.Items.Cast<string>().ToArray()));
                    MessageBox.Show(@"Dosya başarılı bir şekilde kaydedildi.", @"e2F GHDB GUI", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show(@"Kaydedilecek veri bulunamadı.", @"e2F GHDB GUI", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception hata)
            {
                MessageBox.Show(string.Format("İşlem sırasında bir hata oluştu.\r\nOluşan Hata: {0}", hata.Message), @"e2F GHDB GUI", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        private void menuFileClose_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
        private void menuAboutAbout_Click(object sender, EventArgs e)
        {
            FrmAbout about = new FrmAbout();
            about.ShowDialog();
        }
    }
}

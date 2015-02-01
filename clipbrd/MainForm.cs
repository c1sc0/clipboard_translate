using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;
using System.Xml.Linq;
using System.Net;

namespace clipbrd
{
    public partial class MainForm : Form
    {
        //http://pinvoke.net/default.aspx/user32.SetClipboardViewer#
        [DllImport("user32.dll")]
        static extern IntPtr SetClipboardViewer(IntPtr hWndNewViewer);

        [DllImport("user32.dll")]
        static extern IntPtr GetClipboardViewer();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool ChangeClipboardChain(IntPtr hWndRemove, IntPtr hWndNewNext);

        [DllImport("user32.dll", SetLastError = true)]
        static extern int SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        IntPtr hWndNextWindow;


        public MainForm()
        {
            InitializeComponent();
            
        }

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case (0x0001): // WM_CREATE
                    hWndNextWindow = SetClipboardViewer(this.Handle);
                    break;
                case (0x0002): // WM_DESTROY
                    ChangeClipboardChain(this.Handle, hWndNextWindow);
                    break;
                case (0x030D): // WM_CHANGECBCHAIN
                    if (m.WParam == hWndNextWindow)
                        hWndNextWindow = m.LParam;
                    else if (hWndNextWindow != IntPtr.Zero)
                        SendMessage(hWndNextWindow, m.Msg, m.WParam, m.LParam);
                    break;
                case (0x0308): // WM_DRAWCLIPBOARD
                    {
                        DisplayClipboardData();
                        // If the clipboard has changed, this event will be executed.
                    }
                    SendMessage(hWndNextWindow, m.Msg, m.WParam, m.LParam);
                    break;
            }

            base.WndProc(ref m);
        }

        public string TranslateText(string input, string languagePair)
        {
            string url = String.Format("http://www.google.com/translate_t?hl=en&ie=UTF8&text={0}&langpair={1}", input, languagePair);
            WebClient webClient = new WebClient();
            webClient.Encoding = System.Text.Encoding.Default;
            string result = webClient.DownloadString(url);
            result = result.Substring(result.IndexOf("<span title=\"") + "<span title=\"".Length);
            result = result.Substring(result.IndexOf(">") + 1);
            result = result.Substring(0, result.IndexOf("</span>"));
            return string.Format("{0} - {1}",input,result.Trim());

        }

        private int pictureCounter = 0;
        public delegate void ClipBoardDataChangedEvenHandler(object sender, ClipBoardDataChangedEventArgs e);
        public static event ClipBoardDataChangedEvenHandler ClipboardDataChanged;

        public void DisplayClipboardData()
        {
            try
            {
                IDataObject iData = new DataObject();
                iData = Clipboard.GetDataObject();

                
                if (iData.GetDataPresent(DataFormats.Rtf))
                {
                    richTextBox.Rtf = (string)iData.GetData(DataFormats.Rtf);
                    addToHistory((string)iData.GetData(DataFormats.Text));
                    notifyIcon.ShowBalloonTip(10, "Fordítás", TranslateText(richTextBox.Text, "en|hu"), ToolTipIcon.Info);
                }
                else if (iData.GetDataPresent(DataFormats.Text))
                {
                    richTextBox.Text = (string)iData.GetData(DataFormats.Text);
                    addToHistory((string)iData.GetData(DataFormats.Text));
                    notifyIcon.ShowBalloonTip(10, "Fordítás", TranslateText(richTextBox.Text,"en|hu"), ToolTipIcon.Info);
                    Thread thr = new Thread(new ParameterizedThreadStart(ProcessCliboardData));
                    thr.Start(richTextBox.Text);
                }
                else if(iData.GetDataPresent(DataFormats.Bitmap))
                {
                    Thread thr = new Thread(new ParameterizedThreadStart(SaveImageToFile));
                    //notifyIcon.ShowBalloonTip(10, "Infó", "A vágólap tartalma kép.", ToolTipIcon.Info);
                    if(!thr.IsAlive)
                    thr.Start(iData);
                }
                else if (iData.GetDataPresent(DataFormats.FileDrop))
                {
                    richTextBox.Clear();
                    //a vágólapra másolt fájlok elérési útja :)
                    string notifString = "";//azértkell hogy egy stringgé alakítsuk a string tömböt, csak a notifyiconhoz kell 
                    string[] files = (string[])iData.GetData(DataFormats.FileDrop);
                    foreach (string item in files)
                    {
                        richTextBox.AppendText(item + "\n");
                        notifString += item + "\n";
                    }
                    notifyIcon.ShowBalloonTip(10, "Infó", string.Format("A vágólapra másolt fájl vagy mappa elérési útja: {0}",notifString), ToolTipIcon.Info);
                }
                else
                {
                    richTextBox.Text = "[A vágólapon található adat nem rtf vagy txt vagy bitmap vagy egyéb fájl formátumú.]";
                }
                
                
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        public void ProcessCliboardData(object text)
        {
            string data = (string)text;
            if (data.Contains("https"))
            {
                MessageBox.Show(string.Format("Találtam egy hivatkozást: {0}", data));
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Biztos vagy benne?", "Figyelmeztetés", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
            if (result == DialogResult.OK)
            {
                Clipboard.Clear();
            }
        }

        private void aboutToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            AboutBox aboutboxForm = new AboutBox();
            aboutboxForm.ShowInTaskbar = false;
            aboutboxForm.TopMost = true;
            aboutboxForm.ShowDialog();
        }

        public void SaveImageToFile(object data)
        {
            IDataObject iData = (IDataObject)data;
            string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string filename = string.Format("\\image_{0}", pictureCounter);
            string ext = ".png";
            //kép mentése
            Image image = (Image)iData.GetData(DataFormats.Bitmap, true);

            //hogy ne írjunk felül régebben készült fájlokat :)
            while (File.Exists(path + filename + ext))
            {
                string[] newFilename = filename.Split('_');
                string name = newFilename[0];
                string count = newFilename[1];
                filename = name + "_" + (int.Parse(count) + 1);
            }

            image.Save(path + filename + ext, System.Drawing.Imaging.ImageFormat.Png);
            pictureCounter++;
            this.Invoke(new Action(() => richTextBox.Text = string.Format("Kép sikeresen elmentve a következő néven: {0}", filename)));
            notifyIcon.ShowBalloonTip(1000, "Infó", string.Format("Kép sikeresen elmentve a következő néven: {0}", filename), ToolTipIcon.Info);
        }

        private bool writeToxml = true;
        private void historyToolStripMenuItem_Click(object sender, EventArgs e)
        { 
            HistoryForm historyForm = new HistoryForm();
            if (historyForm.ShowDialog() == DialogResult.OK && historyForm.Data != null)
            {
                
                string returned_data = historyForm.Data;
                richTextBox.Text = returned_data;
                writeToxml = false; //máshogy nem tudtam megoldani, mert 2x írta ki, ha ez nem állt itt, vmiért.. :D
                Clipboard.SetData(DataFormats.Text, returned_data);
                writeToxml = true;
            }
        }


        XDocument XDoc = null;
        public void addToHistory(string text)
        {
            
            if (!File.Exists("data.xml")) 
            {
                try
                {
                    XDoc = new XDocument(new XElement("CopyHistoryRoot",
                    new XElement("copyEntry",
                    new XElement("text", "default"),
                    new XElement("date", DateTime.Now.ToLongDateString()),
                    new XElement("time", DateTime.Now.ToLongTimeString())
                    )));
                    XDoc.Save("data.xml");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            
            XDoc = XDocument.Load("data.xml");
            //a fenti kód azért kellett, hogy létezzen a fájl amibe írni akarunk :)
            if (writeToxml)
            {
                XDoc.Root.Add(new XElement("copyEntry",
                    new XElement("text", text),
                    new XElement("date", DateTime.Now.ToLongDateString()),
                    new XElement("time", DateTime.Now.ToLongTimeString())
                    ));
                XDoc.Save("data.xml");
            }
            if (ClipboardDataChanged != null)
            {
                ClipboardDataChanged(null, new ClipBoardDataChangedEventArgs(DateTime.Now));
            } 
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Clipboard.Clear();
        }

        

        private void TrayMinimizerForm_Resize(object sender, EventArgs e)
        {
            notifyIcon.BalloonTipTitle = "ClipBoardViewer";
            notifyIcon.BalloonTipText="Sikeresen minimalizáltad az ablakot. Kattints ide, hogy újra megnyisd.";

            if (FormWindowState.Minimized == this.WindowState)
            {
                notifyIcon.Visible = true;
                notifyIcon.ShowBalloonTip(500);
                this.Hide();
            }
            else if (FormWindowState.Normal == this.WindowState)
            {
                notifyIcon.Visible = false;
            }
        }

        private void notifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
        }   
    }
}

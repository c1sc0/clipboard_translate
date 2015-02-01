using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;
using System.IO;
using System.Threading;

namespace clipbrd
{
    public partial class HistoryForm : Form
    {
        XDocument XDoc = null;
        private string data;

        public string Data
        {
            get { return data; }
            set { data = value; }
        }

        public HistoryForm()
        {
            InitializeComponent();
        }

        private void HistoryForm_Load(object sender, EventArgs e)
        {
            MainForm.ClipboardDataChanged += new MainForm.ClipBoardDataChangedEvenHandler(MainForm_ClipboardDataChanged);
            if (File.Exists("data.xml"))
            {
                
                XDoc = XDocument.Load("data.xml");
            }
            else
            {
                XDoc = new XDocument(new XElement("CopyHistoryRoot",
                    new XElement("copyEntry",
                    new XElement("text","default"),
                    new XElement("date",DateTime.Now.ToLongDateString()),
                    new XElement("time",DateTime.Now.ToLongTimeString())
                    )));
                XDoc.Save("data.xml");
            }
            LoadDataToListBox();
        }

        void MainForm_ClipboardDataChanged(object sender, ClipBoardDataChangedEventArgs e)
        {
            LoadDataToListBox();
        }

        public void LoadDataToListBox()
        {
            listBox.Items.Clear();
            XDoc = XDocument.Load("data.xml");
            var list = from x in XDoc.Root.Descendants("copyEntry")
                       select x;

            foreach (XElement x in list)
            {
                string text = (string)x.Element("text");
                listBox.Items.Add(string.Format("{0}", text));
            }
        }

        private void listBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            data = (string)listBox.SelectedItem;
            //data = data.Remove(0, 8);

            XDoc = XDocument.Load("data.xml");
            var q = from x in XDoc.Root.Descendants("copyEntry")
                    where (string)x.Element("text") == data
                    select x;

            
            XElement choosen = (XElement)q.FirstOrDefault();
            if (choosen != null)
            {
                choosen.Remove();
            }
            else
            {
                data = null;
            }

            XDoc.Save("data.xml");

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void clearHistoryButton_Click(object sender, EventArgs e)
        {
            XDoc.Root.RemoveAll();
            XDoc.Save("data.xml");
            this.Close();
        }
    }
}

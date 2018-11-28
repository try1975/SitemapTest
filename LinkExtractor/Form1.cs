using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using SitemapLib;

namespace LinkExtractor
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (bw.IsBusy) return;
            var path = Path.GetFullPath(tbFileName.Text);
            if (File.Exists(path))
            {
                MessageBox.Show($@"Файл {path} уже есть. Возможна запись только в новый файл.");
                return;
            }
            btnStart.Enabled = false;
            toolStripStatusLabel1.Text = @"Сбор ссылок начат";
            bw.RunWorkerAsync();
        }

        private static void TestLoew(string url, ILinkStorage linkStorage = null)
        {
            var loew = new LoewTest(url, linkStorage);
            loew.Execute();
        }

        private static void TestAbot(string url, ILinkStorage linkStorage = null)
        {
            var abotTest = new AbotTest(url, linkStorage);
            abotTest.Execute();
        }

        private void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            var linkStorage = new FileLinkStorage(tbFileName.Text, bw);

            if (cbSkipSitemap.Checked) TestLoew(tbUrl.Text, linkStorage);
            if (linkStorage.LinkCount != 0) return;
            TestAbot(tbUrl.Text, linkStorage);
        }

        private void bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            toolStripStatusLabel1.Text = $@"{e.UserState}";
            btnStart.Enabled = !bw.IsBusy;
        }

        private void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            toolStripStatusLabel1.Text = @"Завершено";
            btnStart.Enabled = true;
        }
    }
}
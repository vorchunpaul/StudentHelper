using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace StudentHelper
{
    public partial class OpenHtml : Form
    {
        string html;
        string path;
        public OpenHtml(string html, string path)
        {
            InitializeComponent();
            this.html = html;
            this.path = path;
            webBrowser1.DocumentText = html;
        }

        private void scrollToolStripMenuItem_Click(object sender, EventArgs e)
        {
            webBrowser1.Document.Window.ScrollTo(0, 50);
        }

        private void webBrauserToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start(path);
        }
    }
}

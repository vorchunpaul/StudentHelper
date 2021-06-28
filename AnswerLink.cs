using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Lucene.Net.Search;
using Lucene.Net.Documents;
using System.Diagnostics;
using System.Windows.Documents;
using Microsoft.Win32;

namespace StudentHelper
{
    public partial class AnswerLink : UserControl
    {
        const int top_margin = 32;
        const int bot_margin = 32;

        const int right_margin = 32;
        const int left_margin = 32;
        
        Document doc;
        string path;
        string anchor;

        public void resize()
        {
            type_label.Left = Width - right_margin - type_label.Width;

            foreach (Control i in Controls)
            {
                i.MaximumSize = new Size(Width - 32, 0);
            }

            Size = new Size(Width, PreferredSize.Height + 32);
        }

        public AnswerLink(Document doc, float score)
        {
            InitializeComponent();

            this.AutoSize = true;

            this.doc = doc; 
            this.path = doc.Get("_path"); ;
            this.anchor = doc.Get("_anchor");

            var type = doc.Get("_type");
            score_labal.Text = $"{score}";
            type_label.Text = type;

            switch (type)
            {
                case "H1": parse_H(doc, "Дисциплина"); break;
                case "H2": parse_H(doc, "Раздел"); break;
                case "H3": parse_H(doc, "Тема"); break;
                case "H4": parse_H(doc, "Подтема"); break;
                case "DT": parse_DT(doc); break;
                case "OL": parse_OL(doc); break;
                case "P":  parse_P(doc); break;
                default: break;
            }

            resize();
        }

        Label auto_label() { 
            var l = new Label();
            this.Controls.Add(l);
            l.Font = new Font("Calibri", 18);
            l.AutoSize = true;
            return l;
        }
        void parse_H(Document doc, String name) {

            var header = auto_label();

            header.Top = score_labal.Bottom + 16;
            header.Left = score_labal.Left;

            header.Text = $"{name}:  {doc.Get("header")}";
            header.Font = new Font(header.Font, FontStyle.Italic | FontStyle.Bold);

            var val = auto_label();
            val.Left = header.Left;
            val.Top = header.Bottom + 16;
            val.Text = "Содержание";
            val.Font = new Font(val.Font, FontStyle.Italic);
            val.ForeColor = Color.LightGray;

        }
        void parse_DT(Document doc)
        {
            var name = auto_label();

            name.Top = score_labal.Bottom + 16;
            name.Left = score_labal.Left;
            name.Text = doc.Get("def_name");

            name.Font = new Font(name.Font, FontStyle.Bold);

            if (doc.Get("def_type") == "text")
            {
                var val = auto_label();
                val.Top = name.Bottom;
                val.Left = name.Left + 32;
                val.Text = doc.Get("text");
                val.Font = new Font(val.Font, FontStyle.Italic);
            }

            if (doc.Get("def_type") == "list")
            {
                var list = auto_label();

                list.Top = name.Bottom + 10;
                list.Left = name.Left;
                list.Text = "Список";
                list.Font = new Font(name.Font, FontStyle.Italic);
                list.ForeColor = Color.LightGray;
            }
        }

        void parse_P(Document doc)
        {
            var p = auto_label();

            p.Top = score_labal.Bottom + 16;
            p.Left = score_labal.Left;
            p.Text = doc.Get("text");

        }

        void parse_OL(Document doc) {
            var top = score_labal.Bottom + 10;
            var num = 1;
            foreach (var i in doc.GetFields())
            {
                if (i.Name != "item") continue;

                var item = auto_label();

                item.Top = top;
                item.Left = score_labal.Left;
                item.Text = $"{num}. {i.StringValue}";

                num += 1;

                top = item.Bottom;
            }
        }

        private void AnswerLink_DoubleClick(object sender, EventArgs e)
        {
            string browserPath = @"C:\Program Files\Mozilla Firefox\firefox.exe";
            path = path.Replace(@"\", "/");
            string link = "\"file:///" + $"{path}#{anchor}\"";
            Process.Start(browserPath, link);
        }
    }
}

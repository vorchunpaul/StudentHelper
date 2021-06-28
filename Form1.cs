using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Version = Lucene.Net.Util.Version;

using Markdig;
using System.Diagnostics;
using System.Text.RegularExpressions;

using AngleSharp;

namespace StudentHelper
{
    public partial class Form1 : Form
    {

        public Form1()
        {
            InitializeComponent();
            form_resize();
        }

        static string index_path = ".\\index";
        static string browser_path = @"C:\Program Files\Mozilla Firefox\firefox.exe";

        public FSDirectory index_dir = FSDirectory.Open(index_path);
        public StandardAnalyzer analyzer = new StandardAnalyzer(Version.LUCENE_30);

        List<AnswerLink> answers = new List<AnswerLink>();

        void search()
        {
            var indexSearcher = new IndexSearcher(index_dir);

            var all_tags = indexSearcher.IndexReader.GetFieldNames(IndexReader.FieldOption.ALL);

            var queryParser = new MultiFieldQueryParser(
                Version.LUCENE_30,
                //new string[] { "theme" },
                all_tags.ToArray(),
                analyzer
            );

            var term = textBox1.Text;

            var timer = new Stopwatch();
            timer.Start();
            Query query = queryParser.Parse(term);
            TopDocs hits = indexSearcher.Search(query, 10);
            timer.Stop();
            
            var docs = hits.ScoreDocs;

            status_label.Text = $"result count: {docs.Length}        time: {timer.Elapsed.TotalSeconds}";

            if (answers.Count > 0) {
                foreach (var i in answers)
                {
                    i.Dispose();
                }
                answers.Clear();
            }

            foreach (ScoreDoc scoreDoc in docs)
            {
                Document document = indexSearcher.Doc(scoreDoc.Doc);

                var answer = new AnswerLink(document, scoreDoc.Score);

                Controls.Add(answer);
                
                answers.Add(answer);

                form_resize();
            }

            indexSearcher.Dispose();
        }

        void form_resize()
        {
            var centr = Width / 2;
            label1.Left = centr - label1.Width / 2;
            textBox1.Left = centr - textBox1.Width / 2;
            
            var pading = 25;

            centr -= 15;

            button1.Left = centr - button1.Width - 25;
            button2.Left = centr + pading;

            var top = status_label.Top + status_label.PreferredHeight + 10;
            foreach (var i in answers)
            {
                i.Top = top;
                i.Left = status_label.Left;
                i.Width = Width - (32 * 3);
                i.resize();
                top = i.Top + i.Height + pading;
            }
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            form_resize();
        }

        private void start_search_click(object sender, EventArgs e)
        {
            search();
        }

        private void show_index_form(object sender, EventArgs e)
        {
            var indexform = new IndexForm(ref browser_path, index_path);
            indexform.Show();
        }
    }
}

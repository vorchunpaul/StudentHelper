using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
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


namespace StudentHelper
{
    public partial class SearchForm : Form
    {
        Form1 root_form;
        public SearchForm(Form1 root_form)
        {
            InitializeComponent();
            textBox1.Focus();
            this.root_form = root_form;
        }

        void start_search() {
            richTextBox1.Text = null;

            var indexSearcher = new IndexSearcher(root_form.index_dir);

            var all_tags = indexSearcher.IndexReader.GetFieldNames(IndexReader.FieldOption.ALL);

            var queryParser = new MultiFieldQueryParser(
                Version.LUCENE_30,
                //new string[] { "theme" },
                all_tags.ToArray(), 
                root_form.analyzer
            );

            var term = textBox1.Text;

            Query query = queryParser.Parse(term);

            TopDocs hits = indexSearcher.Search(query, 10);

            foreach (ScoreDoc scoreDoc in hits.ScoreDocs)
            {
                Document document = indexSearcher.Doc(scoreDoc.Doc);

                var answer = $"{scoreDoc.Score}\n";
                foreach (var i in document.GetFields())
                {
                    answer += $"\t{i.Name}\t{i.StringValue}\n";
                }

                richTextBox1.Text += answer;
            }

            indexSearcher.Dispose();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            start_search();
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Enter)
            {
                start_search();
            }
        }
    }
}

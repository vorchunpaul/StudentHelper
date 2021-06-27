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
        MarkdownPipeline pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();

        public Form1()
        {
            InitializeComponent();
            form_resize();
        }

        string outputfile;
        string html;

        List<Document> docs = new List<Document>();
        List<AngleSharp.Dom.INode> nodes = new List<AngleSharp.Dom.INode>();

        static string index_path = ".\\index";

        public FSDirectory index_dir = FSDirectory.Open(index_path);
        public StandardAnalyzer analyzer = new StandardAnalyzer(Version.LUCENE_30);

        void md_to_html()
        {

            var file = "db";

            var markdown = File.ReadAllText($"E:\\2021 lecture\\kursach\\{file}.md");

            //richTextBox1.Text = markdown;

            html = Markdown.ToHtml(markdown, pipeline).Replace("\n", "");

            //richTextBox2.Text = html;

            outputfile = $"{file}.html";

            File.WriteAllText(outputfile, html);
        }

        Document init_doc_from_node(AngleSharp.Dom.INode node) {
            
            // save to generate anchors
            nodes.Add(node);

            var index_doc = new Document();
            var hash_id = node.TextContent.GetHashCode().ToString("X");
            new List<Field>() {
                new Field("_type", node.NodeName, Field.Store.YES, Field.Index.ANALYZED),
                new Field("_anchor", hash_id, Field.Store.YES, Field.Index.ANALYZED),
            }.ForEach(x => index_doc.Add(x));

            
            docs.Add(index_doc);
            return index_doc;
        }

        void gen_anchors(AngleSharp.Html.Dom.IHtmlDocument doc) {
            foreach (var node in nodes)
            {
                var hash_id = node.TextContent.GetHashCode().ToString("X");
                var anchor = doc.CreateElement("a");
                anchor.SetAttribute("name", hash_id);
                node.InsertBefore(anchor, node.FirstChild);
            }
        }

        void parse_P(AngleSharp.Dom.INode node) {
            var index = init_doc_from_node(node);
            index.Add(new Field("text", node.TextContent, Field.Store.YES, Field.Index.ANALYZED));
        }

        void parse_headers(AngleSharp.Dom.INode node)
        {
            var index = init_doc_from_node(node);
            index.Add(new Field("header", node.TextContent, Field.Store.YES, Field.Index.ANALYZED));
        }

        void parse_OL(AngleSharp.Dom.INode node) 
        {
            var list = node.ChildNodes;
            var doc = init_doc_from_node(node);
            foreach (var item in list)
            {
                if (!item.FirstChild.HasChildNodes) {
                    doc.Add(new Field("item", item.FirstChild.TextContent, Field.Store.YES, Field.Index.ANALYZED));
                } else
                {
                    var child = item.FirstChild;

                    if (child.NodeName == "DL") {
                        doc.Add(new Field("def_list", child.TextContent.GetHashCode().ToString("X"), Field.Store.YES, Field.Index.ANALYZED));
                        parse_DL(child);
                    }
                }
            }
        }
        void parse_DL(AngleSharp.Dom.INode node) 
        {
            var list = node.ChildNodes;

            for (int i = 0; i + 1 < list.Length; i += 2)
            {

                var def_name = list[i];
                var def_val = list[i + 1].FirstChild;

                var doc = init_doc_from_node(def_name);
                doc.Add(new Field("def_name", def_name.TextContent, Field.Store.YES, Field.Index.ANALYZED));

                if (def_val.NodeName == "P" || def_val.NodeName == "#text")
                {
                    doc.Add(new Field("def_type", "text", Field.Store.YES, Field.Index.ANALYZED));
                    doc.Add(new Field("text", def_val.TextContent, Field.Store.YES, Field.Index.ANALYZED));
                }

                if (def_val.NodeName == "OL") 
                {
                    doc.Add(new Field("def_type", "list", Field.Store.YES, Field.Index.ANALYZED));
                    doc.Add(new Field("list", def_val.TextContent.GetHashCode().ToString("X"), Field.Store.YES, Field.Index.ANALYZED));
                    parse_OL(def_val);
                }
            }
        }

        void create_index()
        {
            md_to_html();

            if (System.IO.Directory.Exists(index_path))
            {
                System.IO.Directory.Delete(index_path, true);
                index_dir = FSDirectory.Open(index_path);
            }

            var parser = new AngleSharp.Html.Parser.HtmlParser(new AngleSharp.Html.Parser.HtmlParserOptions
            {
                IsKeepingSourceReferences = true
            });
            var html_doc = parser.ParseDocument(html);

            var body_nodes = html_doc.QuerySelector("body").ChildNodes;

            foreach (var node in body_nodes)
            {
                var node_name = node.NodeName;

                if (node_name == "H1" || node_name == "H2" || node_name == "H3" || node_name == "H4")
                {
                    parse_headers(node);
                }

                if (node_name == "DL") {
                    parse_DL(node);
                }

                if (node_name == "P") {
                    parse_P(node);
                }
            }

            using (var writer = new IndexWriter(index_dir, analyzer, IndexWriter.MaxFieldLength.UNLIMITED))
            {
                docs.ForEach(x => writer.AddDocument(x));
            }

            gen_anchors(html_doc);

            html = html_doc.ToHtml();
            //richTextBox2.Text = html;
            File.WriteAllText(outputfile, html);
        }

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

                var answer = new AnswerLink(document, scoreDoc.Score, new FileInfo(outputfile).FullName);

                Controls.Add(answer);
                
                answers.Add(answer);

                form_resize();
/*              var answer_labal = new Label();
                answer_labal.Text = "";
                answer_labal.Text = out_ansver;

                answer_labal.Font = new Font("Calibri", 18);
                answer_labal.Top = top;
                answer_labal.AutoSize = true;
                answer_labal.MaximumSize = new Size(this.Width - 32, this.Height);

                top = answer_labal.Top + answer_labal.PreferredHeight + pading;
                Controls.Add(answer_labal);*/
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

            //Size = new Size(Width, PreferredSize.Height + 32);
        }
        private void button1_Click(object sender, EventArgs e)
        {
            
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            create_index();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //Process.Start(outputfile);
            var open = new OpenHtml(html, outputfile);
            open.Show();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            create_index();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            search();
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            form_resize();
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            search();
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            create_index();
        }
    }
}

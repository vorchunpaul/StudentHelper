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
        MarkdownPipeline pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

        public Form1()
        {
            InitializeComponent();
        }

        string outputfile;
        string html;

        static string index_path = ".\\index";

        public FSDirectory index_dir = FSDirectory.Open(index_path);
        public StandardAnalyzer analyzer = new StandardAnalyzer(Version.LUCENE_30);

        void md_to_html() {

            var file = "db";

            var markdown = File.ReadAllText($"E:\\2021 lecture\\kursach\\{file}.md");

            richTextBox1.Text = markdown;

            html = Markdown.ToHtml(markdown, pipeline);

            richTextBox2.Text = html;

            outputfile = $"{file}.html";

            File.WriteAllText(outputfile, html);
        }

        string remove_first_tag(string s) {
            var str = s.Trim();

            var open_tag = Regex.Match(str, ">");

            str = str.Remove(0, open_tag.Index + 1);
            
            var close_tag = Regex.Match(str, "</", RegexOptions.RightToLeft);

            str = str.Remove(close_tag.Index, str.Length - close_tag.Index);

            return str;
        }

        
        void add_tag_to_docs(string tag, string name,  List<Document> docs, AngleSharp.Html.Dom.IHtmlDocument parserd_doc) {
            foreach (var i in parserd_doc.QuerySelectorAll(tag))
            {
                var text = i.TextContent;
                var pos = i.SourceReference?.Position.ToString();

                var doc = new Document();
                doc.Add(new Field("_start", pos, Field.Store.YES, Field.Index.ANALYZED));
                doc.Add(new Field(name, text, Field.Store.YES, Field.Index.ANALYZED));
                docs.Add(doc);
            }    
        }

        void add_items_to_docs(string tag, string name, List<Document> docs, AngleSharp.Html.Dom.IHtmlDocument parserd_doc)
        {
            foreach (var i in parserd_doc.QuerySelectorAll(tag))
            {
                var nodes = i.ChildNodes;
                if (nodes.Length > 1) continue;
                if (i.TextContent == "\n") continue;

                var text = i.TextContent;
                var pos = i.SourceReference?.Position.ToString();

                var doc = new Document();
                doc.Add(new Field("_start", pos, Field.Store.YES, Field.Index.ANALYZED));
                doc.Add(new Field(name, text, Field.Store.YES, Field.Index.ANALYZED));
                docs.Add(doc);
            }
        }

        void add_lists_to_docs(string tag, string name, List<Document> docs, AngleSharp.Html.Dom.IHtmlDocument parserd_doc)
        {
            foreach (var i in parserd_doc.QuerySelectorAll(tag))
            {
                var nodes = i.ChildNodes;
                var pos = i.SourceReference?.Position.ToString();

                var doc = new Document();
                doc.Add(new Field("_start", pos, Field.Store.YES, Field.Index.ANALYZED));

                foreach (var node in nodes)
                {
                    if (node.ChildNodes.Length > 1) continue;
                    if (node.TextContent == "\n") continue;
                    doc.Add(new Field(name, node.TextContent, Field.Store.YES, Field.Index.ANALYZED));
                }

                docs.Add(doc);
            }
        }

        void add_def_to_docs(string tag, string name, string def, string pos, List<Document> docs) {
            var doc = new Document();
            doc.Add(new Field("_start", pos, Field.Store.YES, Field.Index.ANALYZED));
            doc.Add(new Field("tag", tag, Field.Store.YES, Field.Index.ANALYZED));
            doc.Add(new Field("name", name, Field.Store.YES, Field.Index.ANALYZED));
            doc.Add(new Field("def", def, Field.Store.YES, Field.Index.ANALYZED));
            docs.Add(doc);
        }
        void add_defintion_to_docs(string tag, string name, List<Document> docs, AngleSharp.Html.Dom.IHtmlDocument parserd_doc)
        {
            foreach (var deflist in parserd_doc.QuerySelectorAll(tag)) {
                var terms = deflist.QuerySelectorAll("dt");
                var defins = deflist.QuerySelectorAll("dd");

                var term_name = terms[0].TextContent;
                for (int i = 0; i < terms.Length; i++)
                {
                    var defin = defins[i];
                    
                    var term_var = "";
                    var pos = "";

                    if (defin.QuerySelector("ol") != null)
                    {
                        var list = defin.QuerySelectorAll("li");

                        foreach (var li in list)
                        {
                            var root = li
                                .ParentElement  // ol
                                .ParentElement  // dd
                                .ParentElement  // dt
                                .QuerySelector("dt")
                                .TextContent;
                            var check = root == term_name;
                            var def = defin.TextContent;
                            if (check && li.QuerySelector("dl") == null)
                            {
                                term_var = li.TextContent;
                                pos = li.SourceReference.Position.ToString();
                                add_def_to_docs("defin list item", term_name, term_var, pos, docs);
                            }
                        }
                    }
                    else 
                    {
                        term_var = defin.TextContent;
                        pos = defin.SourceReference.Position.ToString();
                        add_def_to_docs("def", terms[i].TextContent, term_var, pos, docs);
                    }
                }
            }
        }

        void create_index() {

            if (System.IO.Directory.Exists(index_path)) {
                System.IO.Directory.Delete(index_path, true);
                index_dir = FSDirectory.Open(index_path);
            }

            var parser = new AngleSharp.Html.Parser.HtmlParser(new AngleSharp.Html.Parser.HtmlParserOptions {
                IsKeepingSourceReferences = true
            });
            var html_doc = parser.ParseDocument(html);

            var docs = new List<Document>();
    
            add_tag_to_docs("h1", "document", docs,  html_doc);
            add_tag_to_docs("h2", "part", docs, html_doc);
            add_tag_to_docs("h3", "theme", docs, html_doc);
            add_tag_to_docs("h4", "subtheme", docs, html_doc);
            add_tag_to_docs("p", "paragraph", docs, html_doc);
            add_items_to_docs("li", "item", docs, html_doc);
            add_lists_to_docs("ol", "list item", docs, html_doc);
            add_defintion_to_docs("dl", "definiton item", docs, html_doc);

            using (var writer = new IndexWriter(index_dir, analyzer, IndexWriter.MaxFieldLength.UNLIMITED))
            {
                foreach (var doc in docs)
                {
                    writer.AddDocument(doc);
                }
            }
            
        }
        void search() {
            var form = new SearchForm(this);
            form.Show();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            md_to_html();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            md_to_html();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Process.Start(outputfile);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            create_index();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            search();
        }
    }
}

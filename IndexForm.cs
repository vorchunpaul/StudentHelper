using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SQLite;
using System.IO;
using System.Security.Cryptography;
using Lucene.Net.Store;
using Markdig;
using Lucene.Net.Index;
using Lucene.Net.Documents;
using AngleSharp;
using Lucene.Net.Analysis.Standard;
using Version = Lucene.Net.Util.Version;
using AngleSharp.Html.Parser;

namespace StudentHelper
{
    public partial class IndexForm : Form
    {
        const string db_path = "studenhelper.db";
        const string html_build_path = @".\html";
       
        string index_path = "";

        string doc_path;

        FSDirectory index_dir;

        SQLiteConnection connection = null;

        StandardAnalyzer analyzer = new StandardAnalyzer(Version.LUCENE_30);

        MarkdownPipeline pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();

        List<Document> docs = new List<Document>();
        List<AngleSharp.Dom.INode> nodes = new List<AngleSharp.Dom.INode>();

        HtmlParser parser = new AngleSharp.Html.Parser.HtmlParser(new AngleSharp.Html.Parser.HtmlParserOptions
        {
            IsKeepingSourceReferences = true
        });

        public IndexForm(ref string browser_path, string index_path)
        {
            InitializeComponent();

            this.index_path = index_path;

            browser_box.Text = browser_path;
            index_dir = FSDirectory.Open(index_path);

            open_db();
        }


        void open_db() {
            connection = new SQLiteConnection($"Data Source={db_path}; Version=3;");
            connection.Open();

            new SQLiteCommand(
                "CREATE TABLE IF NOT EXISTS index_files (" +
                "   file_id INTEGER PRIMARY KEY AUTOINCREMENT," +
                "   name, path, last_check_sha256);",
                connection
            ).ExecuteNonQuery();

            var reader = new SQLiteCommand(
                "SELECT * FROM index_files;",
                connection
            ).ExecuteReader();

            while (reader.Read()) {
                var data = new string[] {
                    reader.GetValue(1).ToString(), // name
                    reader.GetValue(2).ToString(), // full path
                    "?",
                    reader.GetValue(0).ToString(), // id 
                    reader.GetValue(3).ToString(), // sha256
                };

                var row = dataGridView1.Rows.Add(data);
                dataGridView1.Rows[row].DefaultCellStyle.BackColor = Color.Blue;
                dataGridView1.Rows[row].DefaultCellStyle.ForeColor = Color.White;
            }
        }
        void add_file() {

            if (openFileDialog1.ShowDialog() == DialogResult.Cancel) return;

            var fi = new FileInfo(openFileDialog1.FileName);

            new SQLiteCommand(
                $"INSERT INTO index_files (name, path) " +
                $"VALUES (\"{fi.Name}\",\"{fi.FullName}\");",
                connection
            ).ExecuteNonQuery();

            var id = new SQLiteCommand(
                $"SELECT last_insert_rowid();",
                connection
            ).ExecuteScalar();

            var row = dataGridView1.Rows.Add(fi.Name, fi.FullName, "NO", id, "NULL");
            dataGridView1.Rows[row].DefaultCellStyle.BackColor = Color.Red;
            dataGridView1.Rows[row].DefaultCellStyle.ForeColor = Color.White;
        }

        void remove_file() {

            if (dataGridView1.SelectedRows == null) return;

            foreach (DataGridViewCell i in dataGridView1.SelectedCells)
            {
                var row = dataGridView1.Rows[i.RowIndex];
                var id = row.Cells["Id"].Value;
                var comm = $"DELETE FROM index_files " +
                           $"WHERE file_id = {id};";

                new SQLiteCommand(
                    comm,
                    connection
                ).ExecuteNonQuery();

                dataGridView1.Rows.Remove(row);
            }
            
        }

        string sha256_from_file(string path) {

            var hashgen = SHA256.Create();
            var result = "";

            using (FileStream stream = File.OpenRead(path))
            {
                var bytes = hashgen.ComputeHash(stream);
                foreach (byte b in bytes) 
                    result += b.ToString("x2");
            }

            return result;
        } 

        void check_files() {
            foreach (DataGridViewRow i in dataGridView1.Rows)
            {
                var new_hash = sha256_from_file(i.Cells["Path"].Value.ToString());
                var old_hash = i.Cells["sha256"].Value.ToString();
                if (new_hash == old_hash)
                {
                    i.Cells["Check"].Value = "YES";
                    i.DefaultCellStyle.BackColor = Color.Green;
                }
                else {
                    i.Cells["Check"].Value = "NO";
                    i.DefaultCellStyle.BackColor = Color.Red;
                }
            }
        }

        string md_to_html(string soures_md_path, string dir_path)
        {
            var fi = new FileInfo(soures_md_path);

            var markdown = File.ReadAllText(soures_md_path);

            var html = Markdown.ToHtml(markdown, pipeline).Replace("\n", "");

            doc_path = $"{dir_path}\\{fi.Name}.html";

            File.WriteAllText(doc_path, html);

            return sha256_from_file(soures_md_path);
        }

        void regenerate_index()
        {
            if (System.IO.Directory.Exists(html_build_path))
            {
                System.IO.Directory.Delete(html_build_path, true);
            }

            System.IO.Directory.CreateDirectory(html_build_path);

            if (System.IO.Directory.Exists(index_path))
            {
                System.IO.Directory.Delete(index_path, true);
                index_dir = FSDirectory.Open(index_path);
            }

            foreach (DataGridViewRow i in dataGridView1.Rows)
            {
                var md_path = i.Cells["Path"].Value.ToString();
                var hash = md_to_html(md_path, html_build_path);

                create_index(doc_path);

                i.Cells["sha256"].Value = hash;

                var id = i.Cells["Id"].Value;
                var comm = $"UPDATE index_files " +
                           $"SET last_check_sha256 = \"{hash}\" " +
                           $"WHERE file_id = {id};";

                new SQLiteCommand(
                    comm,
                    connection
                ).ExecuteNonQuery();
            }

            check_files();
        }

        void create_index(string html_path)
        {
            var html_doc = parser.ParseDocument(File.ReadAllText(html_path));

            var body_nodes = html_doc.QuerySelector("body").ChildNodes;

            docs.Clear();

            foreach (var node in body_nodes)
            {
                var node_name = node.NodeName;

                if (node_name == "H1" || node_name == "H2" || node_name == "H3" || node_name == "H4")
                {
                    parse_headers(node);
                }

                if (node_name == "DL")
                {
                    parse_DL(node);
                }

                if (node_name == "P")
                {
                    parse_P(node);
                }
            }

            using (var writer = new IndexWriter(index_dir, analyzer, IndexWriter.MaxFieldLength.UNLIMITED))
            {
                docs.ForEach(x => writer.AddDocument(x));
            }

            gen_anchors(html_doc);

            var html = html_doc.ToHtml();

            File.WriteAllText(html_path, html);
        }

        Document init_doc_from_node(AngleSharp.Dom.INode node)
        {

            // save to generate anchors
            nodes.Add(node);

            var index_doc = new Document();
            var hash_id = node.TextContent.GetHashCode().ToString("X");
            new List<Field>() {
                new Field("_type", node.NodeName, Field.Store.YES, Field.Index.ANALYZED),
                new Field("_anchor", hash_id, Field.Store.YES, Field.Index.ANALYZED),
                new Field("_path", new FileInfo(doc_path).FullName, Field.Store.YES, Field.Index.ANALYZED),
            }.ForEach(x => index_doc.Add(x));


            docs.Add(index_doc);
            return index_doc;
        }

        void gen_anchors(AngleSharp.Html.Dom.IHtmlDocument doc)
        {
            foreach (var node in nodes)
            {
                var hash_id = node.TextContent.GetHashCode().ToString("X");
                var anchor = doc.CreateElement("a");
                anchor.SetAttribute("name", hash_id);
                node.InsertBefore(anchor, node.FirstChild);
            }
        }

        void parse_P(AngleSharp.Dom.INode node)
        {
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
                if (!item.FirstChild.HasChildNodes)
                {
                    doc.Add(new Field("item", item.FirstChild.TextContent, Field.Store.YES, Field.Index.ANALYZED));
                }
                else
                {
                    var child = item.FirstChild;

                    if (child.NodeName == "DL")
                    {
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

        private void button3_Click(object sender, EventArgs e)
        {
            add_file();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            remove_file();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            check_files();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            regenerate_index();
        }
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NeutrinoUIDesigner
{
    public partial class Form1 : Form
    {
        List<Item> Items { get; set; }
        string SaveFile { get; set; }
        bool UnsavedWork { get; set; }
        int ScreenWidth { get; set; }
        int ScreenHeight { get; set; }
        public Form1(string[] args)
        {
            InitializeComponent();
            Items = new List<Item>() { new Item("<View>", ""), new Item("WindowInfo", "ID:0;Position X:-1;Position Y:-1;Width:-1;Height:-1;Title:Window;TitleBar:1;MaximizeButton:1;Hidden:0;Maximized:0;StickyDraw:0;WakeOnInteraction:0;") };
            ReloadItems();
            SaveFile = "";
            UnsavedWork = false;
            toolStripTextBox1.Text = "128";
            toolStripTextBox2.Text = "64";
            ScreenWidth = 128;
            ScreenHeight = 64;
            panel3.Width = ScreenWidth * 2;
            panel3.Height = ScreenHeight * 2;
            label1.Location = new Point(10 + ScreenWidth * 2, 10);
            label1.Text = ScreenWidth + "x" + ScreenHeight;
            if(args.Length == 1)
            {
                OpenFile(args[0]);
            }
        }
        private void ReloadItems()
        {
            listBox1.Items.Clear();
            comboBox1.Items.Clear();
            foreach(Item i in Items)
            {
                listBox1.Items.Add(i.Name);
                comboBox1.Items.Add(i.Name);
            }
            panel3.Invalidate();
        }
        private void LoadElementsFromView()
        {
            string v = Items[0].Text;
            List<Element> els = Element.DeserializeView(v);
            Items = new List<Item>() { new Item("<View>", "") };
            foreach(Element el in els)
            {
                Items.Add(new Item(el.GetProperty("Type") + " " + el.GetProperty("ID"), el.Serialize()));
                if(el.GetProperty("Type") == "WindowInfo")
                {
                    toolStripTextBox1.Text = el.GetProperty("Width");
                    toolStripTextBox2.Text = el.GetProperty("Height");
                    SetWindowSize(el.GetPropertyInt("Width"), el.GetPropertyInt("Height"));
                }
            }
            ReloadItems();
            //ShowSerializedView();
            comboBox1.SelectedIndex = 0;
            listBox1.SelectedIndex = 0;
            panel3.Invalidate();
        }
        private void ShowSerializedView()
        {
            List<Element> els = new List<Element>();
            foreach (Item i in Items)
            {
                if (i.Name != "<View>")
                    els.Add(Element.Deserialize(i.Text));
            }
            Items[0].Text = Element.SerializeView(els);
            textBox1.Text = Element.SerializeView(els);
            panel3.Invalidate();
        }
        private void splitContainer1_Panel2_Paint(object sender, PaintEventArgs e)
        {

        }

        private void newViewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFile = "";
            Items = new List<Item>() { new Item("<View>", ""), new Item("WindowInfo", "ID:0;Position X:-1;Position Y:-1;Width:-1;Height:-1;Title:Window;TitleBar:1;MaximizeButton:1;Hidden:0;Maximized:0;StickyDraw:0;WakeOnInteraction:0;") };
            ReloadItems();
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem != null)
            {
                string sel = listBox1.SelectedItem.ToString();
                comboBox1.SelectedItem = sel;
                if (comboBox1.SelectedIndex == 0) ShowSerializedView();
                else
                {
                    string te = "";
                    te = Items[listBox1.SelectedIndex].Text;
                    textBox1.Text = te;
                    Element dec = Element.Deserialize(te);
                    dataGridView1.Rows.Clear();
                    foreach(KeyValuePair<string, string> p in dec.Properties)
                    {
                        DataGridViewRow row = (DataGridViewRow)dataGridView1.Rows[0].Clone();
                        row.Cells[0].Value = p.Key;
                        row.Cells[1].Value = p.Value;
                        dataGridView1.Rows.Add(row);
                    }
                }
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedItem != null)
            {
                string sel = comboBox1.SelectedItem.ToString();
                listBox1.SelectedItem = sel;
                if (listBox1.SelectedIndex == 0) ShowSerializedView();
                else
                {
                    string te = "";
                    te = Items[listBox1.SelectedIndex].Text;
                    textBox1.Text = te;
                    Element dec = Element.Deserialize(te);
                    dataGridView1.Rows.Clear();
                    foreach (KeyValuePair<string, string> p in dec.Properties)
                    {
                        DataGridViewRow row = (DataGridViewRow)dataGridView1.Rows[0].Clone();
                        row.Cells[0].Value = p.Key;
                        row.Cells[1].Value = p.Value;
                        dataGridView1.Rows.Add(row);
                    }
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            int avid = 0;
            foreach(Item i in Items)
            {
                if(i.Name != "<View>")
                {
                    if (Element.Deserialize(i.Text).GetPropertyInt("ID") == avid) avid += 1;
                }
            }
            AddElementDialogueBox aedb = new AddElementDialogueBox(avid);
            DialogResult adr = aedb.ShowDialog();
            if (adr == DialogResult.OK)
            {
                Element el = Element.Deserialize(aedb.ResultingElement);
                Items.Add(new Item(el.GetProperty("Type") + " " + el.GetProperty("ID"), el.Serialize()));
                ReloadItems();
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (((TextBox)sender).Modified && listBox1.SelectedIndex > -1)
            {
                UnsavedWork = true;
                if (comboBox1.SelectedIndex == 0)
                {
                    Items[0].Text = textBox1.Text;
                }
                else Items[listBox1.SelectedIndex].Text = textBox1.Text;
            }
        }

        private void addElementToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int avid = 0;
            foreach (Item i in Items)
            {
                if (i.Name != "<View>")
                {
                    if (Element.Deserialize(i.Text).GetPropertyInt("ID") == avid) avid += 1;
                }
            }
            AddElementDialogueBox aedb = new AddElementDialogueBox(avid);
            DialogResult adr = aedb.ShowDialog();
            if (adr == DialogResult.OK)
            {
                Element el = Element.Deserialize(aedb.ResultingElement);
                Items.Add(new Item(el.GetProperty("Type") + " " + el.GetProperty("ID"), el.Serialize()));
                ReloadItems();
            }
        }

        private void deleteElementToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(comboBox1.SelectedItem.ToString() != "<View>")
            {
                string n = listBox1.SelectedItem.ToString();
                listBox1.Items.RemoveAt(listBox1.SelectedIndex);
                comboBox1.Items.RemoveAt(comboBox1.SelectedIndex);
                for (int i = 0; i < Items.Count(); i++) if (Items[i].Name == n) Items.RemoveAt(i);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (comboBox1.SelectedItem.ToString() != "<View>")
            {
                string n = listBox1.SelectedItem.ToString();
                listBox1.Items.RemoveAt(listBox1.SelectedIndex);
                comboBox1.Items.RemoveAt(comboBox1.SelectedIndex);
                for (int i = 0; i < Items.Count(); i++) if (Items[i].Name == n) Items.RemoveAt(i);
                textBox1.Text = "";
            }
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl1.SelectedIndex == 1)
            {
                if (listBox1.SelectedIndex == 0)
                {
                    ShowSerializedView();
                }
            }
            else if (tabControl1.SelectedIndex == 0)
            {
                if (listBox1.SelectedIndex == 0)
                {
                    LoadElementsFromView();
                }
            }
        }
        private void OpenFile(string file)
        {
            if (Path.GetExtension(file) == ".ns")
            {
                string ser = "";
                List<string> f = new List<string>(File.ReadAllLines(file, Encoding.GetEncoding(1252)));
                for (int i = 0; i < f.Count; i++)
                {
                    if (f[i].StartsWith("spush"))
                    {
                        ser = f[i].Remove(0, 7);
                        break;
                    }
                }
                if (ser.Length > 0)
                {
                    ser = ser.Remove(ser.Length - 1, 1);
                    ser.Replace("\\\"", "\"");
                    Items = new List<Item>() { new Item("<View>", ser) };
                    SaveFile = file;
                    LoadElementsFromView();
                }
                else MessageBox.Show("The selected file is not a valid View Designer source file. Please select another file!", "Invalid file", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            else if(Path.GetExtension(file) == ".py")
            {
                string ser = "";
                List<string> f = new List<string>(File.ReadAllLines(file, Encoding.GetEncoding(1252)));
                for (int i = 0; i < f.Count; i++)
                {
                    if (f[i].TrimStart().StartsWith("text = "))
                    {
                        ser = f[i].TrimStart().Remove(0, 8);
                        break;
                    }
                }
                if (ser.Length > 0)
                {
                    ser = ser.Remove(ser.Length - 1, 1);
                    ser.Replace("\\\"", "\"");
                    Items = new List<Item>() { new Item("<View>", ser) };
                    SaveFile = file;
                    LoadElementsFromView();
                }
                else MessageBox.Show("The selected file is not a valid View Designer source file. Please select another file!", "Invalid file", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            else
            {
                Items = new List<Item>() { new Item("<View>", File.ReadAllText(file)) };
                SaveFile = file;
                LoadElementsFromView();
            }
        }
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog od = new OpenFileDialog();
            od.Filter = "Whiplash Python files (*.py)|*.py|Neutrino Source Files (*.ns)|*.ns|Layout Files (*.txt)|*.txt|All files (*.*)|*.*";
            od.Title = "Open layout";
            if(od.ShowDialog() == DialogResult.OK)
            {
                OpenFile(od.FileName);
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (SaveFile == "") saveAsToolStripMenuItem_Click(sender, e);
            else
            {
                ShowSerializedView();
                Save();
            }
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog sd = new SaveFileDialog();
            sd.Filter = "Whiplash Python files (*.py)|*.py|Neutrino Source Files (*.ns)|*.ns|Layout Files (*.txt)|*.txt|All files (*.*)|*.*";
            sd.Title = "Save layout as";
            if (sd.ShowDialog() == DialogResult.OK)
            {
                ShowSerializedView();
                SaveFile = sd.FileName;
                Save();
            }
        }
        private void Save()
        {
            string name = Path.GetFileNameWithoutExtension(SaveFile);
            if (Path.GetExtension(SaveFile) == ".ns") File.WriteAllText(SaveFile, "; " + name + " View Layout\n\n:" + name + "_CreateView\nspush \"" + Items[0].Text.Replace("\"", "\\\"") + "\"\nleap WMCreateWindow\npop __" + name + "_hwnd ; Do not modify the handle variable!\npush __" + name + "_hwnd\nleap WMSetActiveWindow\nleap WMUpdateView\nret\n\n:" + name + "_DestroyView\npush __" + name + "_hwnd\nleap WMDestroyWindow\nret\n\n; Auto-generated with Neutrino UI Design Tool\n; #include " + name + ".ns\n", Encoding.GetEncoding(1252));
            else if (Path.GetExtension(SaveFile) == ".py") File.WriteAllText(SaveFile, "# " + name + " View Layout\n# Auto-generated with Neutrino UI Design Tool\n# import " + Path.GetFileNameWithoutExtension(SaveFile) + "\n\n!('link userlib.lnx')\ndef " + name + "_create_view():\n\ttext = \"" + Items[0].Text.Replace("\"", "\\\"") + "\"\n\tid = WMCreateWindow(text)\n\tWMSetActiveWindow(id)\n\tWMUpdateView()\n\treturn id\n");
            else File.WriteAllText(SaveFile, Items[0].Text, Encoding.GetEncoding(1252));
            UnsavedWork = false;
        }
        private bool ExitApp()
        {
            if (UnsavedWork)
            {
                DialogResult d = MessageBox.Show("The window contains unsaved changes. Do you want to save the layout file?", "Unsaved changes", MessageBoxButtons.YesNoCancel);
                if (d == DialogResult.Yes)
                {
                    saveToolStripMenuItem_Click(null, null);
                    return true;
                }
                else if (d == DialogResult.No) return true;
                else return false;
            }
            else return true;
        }
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void documentationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new HelpWindow("http://lorinet.rf.gd/neutrino/docs/tools/UIDesigner.php").Show();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new AboutWindow().Show();
        }

        private void dataGridView1_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            UnsavedWork = true;
            Dictionary<string, string> ps = new Dictionary<string, string>();
            for(int i = 0; i < dataGridView1.Rows.Count - 1; i++)
            {
                try
                {
                    ps.Add(dataGridView1.Rows[i].Cells[0].Value.ToString(), dataGridView1.Rows[i].Cells[1].Value.ToString());
                }
                catch { }
            }
            Items[listBox1.SelectedIndex].Text = new Element(ps).Serialize();
            textBox1.Text = Items[listBox1.SelectedIndex].Text;
            panel3.Invalidate();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            if (ExitApp()) e.Cancel = false;
        }
        private void ShowPreview(ref Graphics g)
        {
            List<Element> e = new List<Element>();
            for(int i = 1; i < Items.Count; i++)
            {
                e.Add(Element.Deserialize(Items[i].Text));
            }
            Brush b = Brushes.White;
            Brush tb = Brushes.Azure;
            foreach(Element l in e)
            {
                int w = 0;
                int h = 0;
                if (l.GetPropertyInt("Width") != 0) w = l.GetPropertyInt("Width");
                else w = l.GetProperty("Text").Length * 5;
                if (l.GetPropertyInt("Height") != 0) h = l.GetPropertyInt("Height");
                else h = GetFontSize(l.GetProperty("Font"));
                g.DrawRectangle(new Pen(b), l.GetPropertyInt("Position X") * 2, l.GetPropertyInt("Position Y") * 2, w * 2, h * 2);
                g.DrawString(l.GetProperty("Text"), panel3.Font, tb, l.GetPropertyInt("Position X") * 2, l.GetPropertyInt("Position Y") * 2);
            }
        }
        private int GetFontSize(string font)
        {
            return int.Parse(Regex.Match(font, @"\d+").Value);
        }

        private void panel3_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = panel3.CreateGraphics();
            ShowPreview(ref g);
        }

        private void toolStripTextBox1_Click_1(object sender, EventArgs e)
        {

        }

        private void toolStripTextBox2_Click(object sender, EventArgs e)
        {

        }

        private void applyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetWindowSize(int.Parse(toolStripTextBox1.Text), int.Parse(toolStripTextBox2.Text));
        }

        private void SetWindowSize(int w, int h)
        {
            ScreenWidth = w;
            ScreenHeight = h;
            panel3.Width = ScreenWidth * 2;
            panel3.Height = ScreenHeight * 2;
            label1.Location = new Point(10 + ScreenWidth * 2, 10);
            label1.Text = ScreenWidth + "x" + ScreenHeight;
            panel3.Invalidate();
        }
    }
}

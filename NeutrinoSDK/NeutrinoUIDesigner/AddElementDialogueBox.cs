using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NeutrinoUIDesigner
{
    public partial class AddElementDialogueBox : Form
    {
        public string ResultingElement { get; set; }
        public AddElementDialogueBox(int eid)
        {
            InitializeComponent();
            DialogResult = DialogResult.Cancel;
            ElementIDBox.Value = eid;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Element el = new Element();
            el.SetProperty("ID", ElementIDBox.Value.ToString());
            el.SetProperty("Type", TypeBox.Text);
            el.SetProperty("Position X", PosXBox.Value.ToString());
            el.SetProperty("Position Y", PosYBox.Value.ToString());
            el.SetProperty("Width", WidthBox.Value.ToString());
            el.SetProperty("Height", HeightBox.Value.ToString());
            if (ElementTextBox.Text.Length > 0)
                el.SetProperty("Text", ElementTextBox.Text.Replace(":", "\\:").Replace(";", "\\;").Replace("|", "\\|"));
            if (FontBox.Text.Length > 0)
                el.SetProperty("Font", FontBox.Text.Replace(":", "\\:").Replace(";", "\\;").Replace("|", "\\|"));
            if (TypeBox.Text == "Button" || TypeBox.Text == "CheckBox")
            {
                el.SetProperty("Selectable", "1");
            }
            if(TypeBox.Text == "Label" || TypeBox.Text == "TextField")
            {
                if (BorderBox.Checked) el.SetProperty("Border", "1");
                else el.SetProperty("Border", "0");
            }
            if(TypeBox.Text == "ListView")
            {
                el.SetProperty("Items", itemsBox.Text.Replace("\r", "").Replace("\n", ",") + ",");
            }
            ResultingElement = el.Serialize();
            DialogResult = DialogResult.OK;
            Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}

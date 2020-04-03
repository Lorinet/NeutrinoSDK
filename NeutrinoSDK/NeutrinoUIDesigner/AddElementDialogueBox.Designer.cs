namespace NeutrinoUIDesigner
{
    partial class AddElementDialogueBox
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.ElementIDBox = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.TypeBox = new System.Windows.Forms.ComboBox();
            this.PosXBox = new System.Windows.Forms.NumericUpDown();
            this.label3 = new System.Windows.Forms.Label();
            this.PosYBox = new System.Windows.Forms.NumericUpDown();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.ElementTextBox = new System.Windows.Forms.TextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.FontBox = new System.Windows.Forms.ComboBox();
            this.label6 = new System.Windows.Forms.Label();
            this.HeightBox = new System.Windows.Forms.NumericUpDown();
            this.label7 = new System.Windows.Forms.Label();
            this.WidthBox = new System.Windows.Forms.NumericUpDown();
            this.label8 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.ElementIDBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.PosXBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.PosYBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.HeightBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.WidthBox)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(64, 15);
            this.label1.TabIndex = 0;
            this.label1.Text = "Element ID";
            // 
            // ElementIDBox
            // 
            this.ElementIDBox.Location = new System.Drawing.Point(82, 8);
            this.ElementIDBox.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.ElementIDBox.Name = "ElementIDBox";
            this.ElementIDBox.Size = new System.Drawing.Size(181, 20);
            this.ElementIDBox.TabIndex = 1;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(44, 39);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(32, 15);
            this.label2.TabIndex = 2;
            this.label2.Text = "Type";
            // 
            // TypeBox
            // 
            this.TypeBox.FormattingEnabled = true;
            this.TypeBox.Items.AddRange(new object[] {
            "Label",
            "Button",
            "CheckBox",
            "ToggleButton",
            "TextField",
            "TextBox",
            "ListBox",
            "ComboBox",
            "Image"});
            this.TypeBox.Location = new System.Drawing.Point(82, 37);
            this.TypeBox.Name = "TypeBox";
            this.TypeBox.Size = new System.Drawing.Size(181, 21);
            this.TypeBox.TabIndex = 3;
            // 
            // PosXBox
            // 
            this.PosXBox.Location = new System.Drawing.Point(82, 64);
            this.PosXBox.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.PosXBox.Name = "PosXBox";
            this.PosXBox.Size = new System.Drawing.Size(181, 20);
            this.PosXBox.TabIndex = 5;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(16, 65);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(60, 15);
            this.label3.TabIndex = 4;
            this.label3.Text = "Position X";
            // 
            // PosYBox
            // 
            this.PosYBox.Location = new System.Drawing.Point(82, 90);
            this.PosYBox.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.PosYBox.Name = "PosYBox";
            this.PosYBox.Size = new System.Drawing.Size(181, 20);
            this.PosYBox.TabIndex = 7;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(16, 91);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(60, 15);
            this.label4.TabIndex = 6;
            this.label4.Text = "Position Y";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(48, 170);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(28, 15);
            this.label5.TabIndex = 8;
            this.label5.Text = "Text";
            // 
            // ElementTextBox
            // 
            this.ElementTextBox.Location = new System.Drawing.Point(82, 168);
            this.ElementTextBox.Multiline = true;
            this.ElementTextBox.Name = "ElementTextBox";
            this.ElementTextBox.Size = new System.Drawing.Size(181, 145);
            this.ElementTextBox.TabIndex = 9;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(145, 359);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(118, 34);
            this.button1.TabIndex = 10;
            this.button1.Text = "Cancel";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(15, 359);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(124, 34);
            this.button2.TabIndex = 11;
            this.button2.Text = "OK";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // FontBox
            // 
            this.FontBox.FormattingEnabled = true;
            this.FontBox.Items.AddRange(new object[] {
            "Micro 4",
            "Micro 5",
            "Console 7",
            "Times New Roman 7",
            "Times New Roman 7 Bold",
            "Helvetica 8",
            "Helvetica 8 Bold",
            "Open Iconic 8",
            "Crox 9",
            "Crox 9 Bold",
            "Profont 9",
            "Times New Roman 10",
            "Times New Roman 10 Bold",
            "Console 10",
            "Profont 11",
            "Courier New 11",
            "Helvetica 11",
            "Helvetica 11 Bold",
            "Helvetica 12",
            "Helvetica 12 Thin",
            "Times New Roman 13",
            "Times New Roman 13 Bold",
            "Helvetica 14",
            "Inconsolata 16",
            "Logisoso 16",
            "Open Iconic 16",
            "Courier New 19",
            "Helvetica 19",
            "Inconsolata 19",
            "Battery 19",
            "Logisoso 20",
            "Logisoso 22",
            "Times New Roman 23",
            "Helvetica 25",
            "Helvetica 25 Bold",
            "Freedoom 26 Numbers",
            "Logisoso 26",
            "Logisoso 28",
            "Logisoso 32",
            "Open Iconic 32",
            "Logisoso 38",
            "Segments 42 Numbers",
            "Open Iconic 48",
            "Open Iconic 64"});
            this.FontBox.Location = new System.Drawing.Point(82, 319);
            this.FontBox.Name = "FontBox";
            this.FontBox.Size = new System.Drawing.Size(181, 21);
            this.FontBox.TabIndex = 13;
            this.FontBox.SelectedIndexChanged += new System.EventHandler(this.FontBox_SelectedIndexChanged);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.Location = new System.Drawing.Point(44, 321);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(31, 15);
            this.label6.TabIndex = 12;
            this.label6.Text = "Font";
            // 
            // HeightBox
            // 
            this.HeightBox.Location = new System.Drawing.Point(82, 142);
            this.HeightBox.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.HeightBox.Name = "HeightBox";
            this.HeightBox.Size = new System.Drawing.Size(181, 20);
            this.HeightBox.TabIndex = 17;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label7.Location = new System.Drawing.Point(33, 143);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(43, 15);
            this.label7.TabIndex = 16;
            this.label7.Text = "Height";
            // 
            // WidthBox
            // 
            this.WidthBox.Location = new System.Drawing.Point(82, 116);
            this.WidthBox.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.WidthBox.Name = "WidthBox";
            this.WidthBox.Size = new System.Drawing.Size(181, 20);
            this.WidthBox.TabIndex = 15;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label8.Location = new System.Drawing.Point(36, 117);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(39, 15);
            this.label8.TabIndex = 14;
            this.label8.Text = "Width";
            // 
            // AddElementDialogueBox
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(275, 403);
            this.Controls.Add(this.HeightBox);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.WidthBox);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.FontBox);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.ElementTextBox);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.PosYBox);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.PosXBox);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.TypeBox);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.ElementIDBox);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "AddElementDialogueBox";
            this.ShowIcon = false;
            this.Text = "Add Element";
            ((System.ComponentModel.ISupportInitialize)(this.ElementIDBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.PosXBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.PosYBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.HeightBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.WidthBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown ElementIDBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox TypeBox;
        private System.Windows.Forms.NumericUpDown PosXBox;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown PosYBox;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox ElementTextBox;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.ComboBox FontBox;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.NumericUpDown HeightBox;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.NumericUpDown WidthBox;
        private System.Windows.Forms.Label label8;
    }
}
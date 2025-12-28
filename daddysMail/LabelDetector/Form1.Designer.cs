namespace FindLabel
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            originalPicture = new PictureBox();
            button2 = new Button();
            txtPackageLocation = new TextBox();
            label1 = new Label();
            label2 = new Label();
            txtOutputLocation = new TextBox();
            label3 = new Label();
            label4 = new Label();
            txtSize = new TextBox();
            processedPicture = new PictureBox();
            cmbPattern = new ComboBox();
            label5 = new Label();
            txtProcessTime = new TextBox();
            labelPicture = new PictureBox();
            ((System.ComponentModel.ISupportInitialize)originalPicture).BeginInit();
            ((System.ComponentModel.ISupportInitialize)processedPicture).BeginInit();
            ((System.ComponentModel.ISupportInitialize)labelPicture).BeginInit();
            SuspendLayout();
            // 
            // originalPicture
            // 
            originalPicture.Location = new Point(0, 163);
            originalPicture.Name = "originalPicture";
            originalPicture.Size = new Size(441, 287);
            originalPicture.TabIndex = 1;
            originalPicture.TabStop = false;
            // 
            // button2
            // 
            button2.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            button2.Location = new Point(26, 113);
            button2.Name = "button2";
            button2.Size = new Size(1025, 23);
            button2.TabIndex = 2;
            button2.Text = "Start Process";
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click;
            // 
            // txtPackageLocation
            // 
            txtPackageLocation.Location = new Point(126, 10);
            txtPackageLocation.Name = "txtPackageLocation";
            txtPackageLocation.Size = new Size(475, 23);
            txtPackageLocation.TabIndex = 3;
            txtPackageLocation.Text = "C:\\Users\\avshalom.lavi.okabi\\Downloads\\20251211_Bitunia_1";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(17, 18);
            label1.Name = "label1";
            label1.Size = new Size(103, 15);
            label1.TabIndex = 4;
            label1.Text = "Package location :";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(17, 53);
            label2.Name = "label2";
            label2.Size = new Size(100, 15);
            label2.TabIndex = 5;
            label2.Text = "Output location : ";
            // 
            // txtOutputLocation
            // 
            txtOutputLocation.Location = new Point(126, 45);
            txtOutputLocation.Name = "txtOutputLocation";
            txtOutputLocation.Size = new Size(475, 23);
            txtOutputLocation.TabIndex = 6;
            txtOutputLocation.Text = "C:\\Users\\avshalom.lavi.okabi\\Downloads\\20251211_Bitunia_1\\outputs";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.ImageAlign = ContentAlignment.BottomCenter;
            label3.Location = new Point(17, 87);
            label3.Name = "label3";
            label3.Size = new Size(84, 15);
            label3.TabIndex = 7;
            label3.Text = "Pattern name :";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(609, 18);
            label4.Name = "label4";
            label4.Size = new Size(79, 15);
            label4.TabIndex = 10;
            label4.Text = "Package size :";
            // 
            // txtSize
            // 
            txtSize.Location = new Point(688, 10);
            txtSize.Name = "txtSize";
            txtSize.Size = new Size(475, 23);
            txtSize.TabIndex = 9;
            txtSize.TextChanged += textBox1_TextChanged;
            // 
            // processedPicture
            // 
            processedPicture.Anchor = AnchorStyles.None;
            processedPicture.BackgroundImageLayout = ImageLayout.Center;
            processedPicture.Location = new Point(462, 163);
            processedPicture.Name = "processedPicture";
            processedPicture.Size = new Size(441, 287);
            processedPicture.TabIndex = 11;
            processedPicture.TabStop = false;
            // 
            // cmbPattern
            // 
            cmbPattern.FormattingEnabled = true;
            cmbPattern.Items.AddRange(new object[] { "DHL", "IsraelPostOffice", "FED-EX" });
            cmbPattern.Location = new Point(126, 79);
            cmbPattern.Name = "cmbPattern";
            cmbPattern.Size = new Size(268, 23);
            cmbPattern.TabIndex = 12;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(609, 92);
            label5.Name = "label5";
            label5.Size = new Size(80, 15);
            label5.TabIndex = 14;
            label5.Text = "Process time :";
            // 
            // txtProcessTime
            // 
            txtProcessTime.Location = new Point(689, 84);
            txtProcessTime.Name = "txtProcessTime";
            txtProcessTime.Size = new Size(172, 23);
            txtProcessTime.TabIndex = 13;
            // 
            // labelPicture
            // 
            labelPicture.Location = new Point(0, 467);
            labelPicture.Name = "labelPicture";
            labelPicture.Size = new Size(441, 287);
            labelPicture.TabIndex = 15;
            labelPicture.TabStop = false;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1037, 602);
            Controls.Add(labelPicture);
            Controls.Add(label5);
            Controls.Add(txtProcessTime);
            Controls.Add(cmbPattern);
            Controls.Add(processedPicture);
            Controls.Add(label4);
            Controls.Add(txtSize);
            Controls.Add(label3);
            Controls.Add(txtOutputLocation);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(txtPackageLocation);
            Controls.Add(button2);
            Controls.Add(originalPicture);
            Name = "Form1";
            Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)originalPicture).EndInit();
            ((System.ComponentModel.ISupportInitialize)processedPicture).EndInit();
            ((System.ComponentModel.ISupportInitialize)labelPicture).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private PictureBox originalPicture;
        private Button button2;
        private TextBox txtPackageLocation;
        private Label label1;
        private Label label2;
        private TextBox txtOutputLocation;
        private Label label3;
        private Label label4;
        private TextBox txtSize;
        private PictureBox processedPicture;
        private ComboBox cmbPattern;
        private Label label5;
        private TextBox txtProcessTime;
        private PictureBox labelPicture;
    }
}

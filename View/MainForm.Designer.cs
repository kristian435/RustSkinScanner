namespace RustSkinScanner.View
{
    partial class MainForm
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
            components = new System.ComponentModel.Container();
            dgvDeals = new DataGridView();
            btnScan = new Button();
            timerScan = new System.Windows.Forms.Timer(components);
            ((System.ComponentModel.ISupportInitialize)dgvDeals).BeginInit();
            SuspendLayout();
            // 
            // dgvDeals
            // 
            dgvDeals.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvDeals.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvDeals.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvDeals.Location = new Point(12, 141);
            dgvDeals.Name = "dgvDeals";
            dgvDeals.RowHeadersWidth = 62;
            dgvDeals.Size = new Size(1012, 265);
            dgvDeals.TabIndex = 0;
            // 
            // btnScan
            // 
            btnScan.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnScan.Location = new Point(912, 412);
            btnScan.Name = "btnScan";
            btnScan.Size = new Size(112, 34);
            btnScan.TabIndex = 1;
            btnScan.Text = "Scan";
            btnScan.UseVisualStyleBackColor = true;
            btnScan.Click += btnScan_Click;
            // 
            // timerScan
            // 
            timerScan.Enabled = true;
            timerScan.Interval = 900000;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1036, 450);
            Controls.Add(btnScan);
            Controls.Add(dgvDeals);
            Name = "MainForm";
            Text = "Form1";
            Click += btnScan_Click;
            ((System.ComponentModel.ISupportInitialize)dgvDeals).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private DataGridView dgvDeals;
        private Button btnScan;
        private System.Windows.Forms.Timer timerScan;
    }
}

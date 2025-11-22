namespace TrinityFileExplorer
{
    partial class ExportProgressWindow
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
            NameLabel = new Label();
            ToLabel = new Label();
            progressBar1 = new ProgressBar();
            ItemsLabel = new Label();
            CancelExportButton = new Button();
            NameValueLabel = new Label();
            ToValueLabel = new Label();
            SuspendLayout();
            // 
            // NameLabel
            // 
            NameLabel.AutoSize = true;
            NameLabel.Location = new Point(12, 9);
            NameLabel.Name = "NameLabel";
            NameLabel.Size = new Size(42, 15);
            NameLabel.TabIndex = 0;
            NameLabel.Text = "Name:";
            // 
            // ToLabel
            // 
            ToLabel.AutoSize = true;
            ToLabel.Location = new Point(12, 24);
            ToLabel.Name = "ToLabel";
            ToLabel.Size = new Size(22, 15);
            ToLabel.TabIndex = 1;
            ToLabel.Text = "To:";
            // 
            // progressBar1
            // 
            progressBar1.Location = new Point(12, 72);
            progressBar1.Name = "progressBar1";
            progressBar1.Size = new Size(249, 23);
            progressBar1.Style = ProgressBarStyle.Continuous;
            progressBar1.TabIndex = 2;
            // 
            // ItemsLabel
            // 
            ItemsLabel.AutoSize = true;
            ItemsLabel.Location = new Point(12, 54);
            ItemsLabel.Name = "ItemsLabel";
            ItemsLabel.Size = new Size(99, 15);
            ItemsLabel.TabIndex = 4;
            ItemsLabel.Text = "Items Remaining:";
            // 
            // CancelExportButton
            // 
            CancelExportButton.Location = new Point(186, 101);
            CancelExportButton.Name = "CancelExportButton";
            CancelExportButton.Size = new Size(75, 23);
            CancelExportButton.TabIndex = 5;
            CancelExportButton.Text = "Cancel";
            CancelExportButton.UseVisualStyleBackColor = true;
            CancelExportButton.Click += CancelExportButton_Click;
            // 
            // NameValueLabel
            // 
            NameValueLabel.AutoSize = true;
            NameValueLabel.Location = new Point(60, 9);
            NameValueLabel.Name = "NameValueLabel";
            NameValueLabel.Size = new Size(39, 15);
            NameValueLabel.TabIndex = 6;
            NameValueLabel.Text = "Name";
            // 
            // ToValueLabel
            // 
            ToValueLabel.AutoSize = true;
            ToValueLabel.Location = new Point(60, 24);
            ToValueLabel.Name = "ToValueLabel";
            ToValueLabel.Size = new Size(19, 15);
            ToValueLabel.TabIndex = 7;
            ToValueLabel.Text = "To";
            // 
            // ExportProgressWindow
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(273, 132);
            Controls.Add(ToValueLabel);
            Controls.Add(NameValueLabel);
            Controls.Add(CancelExportButton);
            Controls.Add(ItemsLabel);
            Controls.Add(progressBar1);
            Controls.Add(ToLabel);
            Controls.Add(NameLabel);
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            Name = "ExportProgressWindow";
            Text = "Exporting Files";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label NameLabel;
        private Label ToLabel;
        private ProgressBar progressBar1;
        private Label ItemsLabel;
        private Button CancelExportButton;
        private Label NameValueLabel;
        private Label ToValueLabel;
    }
}
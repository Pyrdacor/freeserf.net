namespace FreeserfPatcher
{
    partial class MainForm
    {
        /// <summary>
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            this.ProgressBarPatch = new System.Windows.Forms.ProgressBar();
            this.LabelProgress = new System.Windows.Forms.Label();
            this.LabelText = new System.Windows.Forms.Label();
            this.ButtonQuit = new System.Windows.Forms.Button();
            this.ButtonSkip = new System.Windows.Forms.Button();
            this.ButtonPatch = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // ProgressBarPatch
            // 
            this.ProgressBarPatch.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ProgressBarPatch.Enabled = false;
            this.ProgressBarPatch.Location = new System.Drawing.Point(13, 160);
            this.ProgressBarPatch.Name = "ProgressBarPatch";
            this.ProgressBarPatch.Size = new System.Drawing.Size(371, 23);
            this.ProgressBarPatch.TabIndex = 0;
            // 
            // LabelProgress
            // 
            this.LabelProgress.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.LabelProgress.AutoSize = true;
            this.LabelProgress.Location = new System.Drawing.Point(14, 141);
            this.LabelProgress.Name = "LabelProgress";
            this.LabelProgress.Size = new System.Drawing.Size(35, 13);
            this.LabelProgress.TabIndex = 1;
            this.LabelProgress.Text = "label1";
            this.LabelProgress.Visible = false;
            // 
            // LabelText
            // 
            this.LabelText.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.LabelText.Font = new System.Drawing.Font("Lucida Sans Unicode", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LabelText.Location = new System.Drawing.Point(13, 9);
            this.LabelText.Name = "LabelText";
            this.LabelText.Size = new System.Drawing.Size(370, 121);
            this.LabelText.TabIndex = 2;
            this.LabelText.Text = "A new patch is available.";
            // 
            // ButtonQuit
            // 
            this.ButtonQuit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.ButtonQuit.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.ButtonQuit.Location = new System.Drawing.Point(308, 189);
            this.ButtonQuit.Name = "ButtonQuit";
            this.ButtonQuit.Size = new System.Drawing.Size(75, 23);
            this.ButtonQuit.TabIndex = 3;
            this.ButtonQuit.Text = "Quit";
            this.ButtonQuit.UseVisualStyleBackColor = true;
            this.ButtonQuit.Click += new System.EventHandler(this.ButtonQuit_Click);
            // 
            // ButtonSkip
            // 
            this.ButtonSkip.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.ButtonSkip.Location = new System.Drawing.Point(227, 189);
            this.ButtonSkip.Name = "ButtonSkip";
            this.ButtonSkip.Size = new System.Drawing.Size(75, 23);
            this.ButtonSkip.TabIndex = 4;
            this.ButtonSkip.Text = "Skip";
            this.ButtonSkip.UseVisualStyleBackColor = true;
            this.ButtonSkip.Click += new System.EventHandler(this.ButtonSkip_Click);
            // 
            // ButtonPatch
            // 
            this.ButtonPatch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.ButtonPatch.Location = new System.Drawing.Point(146, 189);
            this.ButtonPatch.Name = "ButtonPatch";
            this.ButtonPatch.Size = new System.Drawing.Size(75, 23);
            this.ButtonPatch.TabIndex = 5;
            this.ButtonPatch.Text = "Patch";
            this.ButtonPatch.UseVisualStyleBackColor = true;
            this.ButtonPatch.Click += new System.EventHandler(this.ButtonPatch_Click);
            // 
            // MainForm
            // 
            this.AcceptButton = this.ButtonPatch;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.CancelButton = this.ButtonQuit;
            this.ClientSize = new System.Drawing.Size(395, 221);
            this.Controls.Add(this.ButtonPatch);
            this.Controls.Add(this.ButtonSkip);
            this.Controls.Add(this.ButtonQuit);
            this.Controls.Add(this.LabelText);
            this.Controls.Add(this.LabelProgress);
            this.Controls.Add(this.ProgressBarPatch);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.Text = "Freeserf.net Patcher";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MainForm_FormClosed);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ProgressBar ProgressBarPatch;
        private System.Windows.Forms.Label LabelProgress;
        private System.Windows.Forms.Label LabelText;
        private System.Windows.Forms.Button ButtonQuit;
        private System.Windows.Forms.Button ButtonSkip;
        private System.Windows.Forms.Button ButtonPatch;
    }
}


namespace Freeserf
{
    partial class CrashHandlerForm
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
            this.ButtonClose = new System.Windows.Forms.Button();
            this.ButtonRestart = new System.Windows.Forms.Button();
            this.ButtonSendReport = new System.Windows.Forms.Button();
            this.TextMessage = new System.Windows.Forms.TextBox();
            this.ButtonSave = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // ButtonClose
            // 
            this.ButtonClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.ButtonClose.DialogResult = System.Windows.Forms.DialogResult.Abort;
            this.ButtonClose.Location = new System.Drawing.Point(377, 131);
            this.ButtonClose.Name = "ButtonClose";
            this.ButtonClose.Size = new System.Drawing.Size(75, 23);
            this.ButtonClose.TabIndex = 0;
            this.ButtonClose.Text = "Close";
            this.ButtonClose.UseVisualStyleBackColor = true;
            // 
            // ButtonRestart
            // 
            this.ButtonRestart.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.ButtonRestart.DialogResult = System.Windows.Forms.DialogResult.Retry;
            this.ButtonRestart.Location = new System.Drawing.Point(296, 131);
            this.ButtonRestart.Name = "ButtonRestart";
            this.ButtonRestart.Size = new System.Drawing.Size(75, 23);
            this.ButtonRestart.TabIndex = 1;
            this.ButtonRestart.Text = "Restart";
            this.ButtonRestart.UseVisualStyleBackColor = true;
            // 
            // ButtonSendReport
            // 
            this.ButtonSendReport.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.ButtonSendReport.Location = new System.Drawing.Point(12, 131);
            this.ButtonSendReport.Name = "ButtonSendReport";
            this.ButtonSendReport.Size = new System.Drawing.Size(134, 23);
            this.ButtonSendReport.TabIndex = 2;
            this.ButtonSendReport.Text = "Send Crash Report";
            this.ButtonSendReport.UseVisualStyleBackColor = true;
            this.ButtonSendReport.Click += new System.EventHandler(this.ButtonSendReport_Click);
            // 
            // TextMessage
            // 
            this.TextMessage.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.TextMessage.Location = new System.Drawing.Point(12, 12);
            this.TextMessage.Multiline = true;
            this.TextMessage.Name = "TextMessage";
            this.TextMessage.ReadOnly = true;
            this.TextMessage.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.TextMessage.Size = new System.Drawing.Size(440, 113);
            this.TextMessage.TabIndex = 3;
            // 
            // ButtonSave
            // 
            this.ButtonSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.ButtonSave.Location = new System.Drawing.Point(152, 131);
            this.ButtonSave.Name = "ButtonSave";
            this.ButtonSave.Size = new System.Drawing.Size(108, 23);
            this.ButtonSave.TabIndex = 4;
            this.ButtonSave.Text = "Save Game";
            this.ButtonSave.UseVisualStyleBackColor = true;
            this.ButtonSave.Click += new System.EventHandler(this.ButtonSave_Click);
            // 
            // CrashHandlerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(464, 166);
            this.Controls.Add(this.ButtonSave);
            this.Controls.Add(this.TextMessage);
            this.Controls.Add(this.ButtonSendReport);
            this.Controls.Add(this.ButtonRestart);
            this.Controls.Add(this.ButtonClose);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.MinimumSize = new System.Drawing.Size(480, 200);
            this.Name = "CrashHandlerForm";
            this.Text = "Freeserf.net Critical Error";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button ButtonClose;
        private System.Windows.Forms.Button ButtonRestart;
        private System.Windows.Forms.Button ButtonSendReport;
        private System.Windows.Forms.TextBox TextMessage;
        private System.Windows.Forms.Button ButtonSave;
    }
}
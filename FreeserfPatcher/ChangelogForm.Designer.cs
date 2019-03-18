namespace FreeserfPatcher
{
    partial class ChangelogForm
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
            this.TextChangelog = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // TextChangelog
            // 
            this.TextChangelog.BackColor = System.Drawing.Color.White;
            this.TextChangelog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TextChangelog.Location = new System.Drawing.Point(0, 0);
            this.TextChangelog.Multiline = true;
            this.TextChangelog.Name = "TextChangelog";
            this.TextChangelog.ReadOnly = true;
            this.TextChangelog.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.TextChangelog.Size = new System.Drawing.Size(544, 406);
            this.TextChangelog.TabIndex = 0;
            // 
            // ChangelogForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(544, 406);
            this.Controls.Add(this.TextChangelog);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.MaximizeBox = false;
            this.MinimumSize = new System.Drawing.Size(560, 440);
            this.Name = "ChangelogForm";
            this.Text = "Changelog";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox TextChangelog;
    }
}
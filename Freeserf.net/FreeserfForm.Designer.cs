namespace Freeserf
{
    partial class FreeserfForm
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FreeserfForm));
            this.RenderControl = new OpenTK.GLControl();
            this.FrameTimer = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // RenderControl
            // 
            this.RenderControl.BackColor = System.Drawing.Color.Black;
            this.RenderControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.RenderControl.Location = new System.Drawing.Point(0, 0);
            this.RenderControl.Name = "RenderControl";
            this.RenderControl.Size = new System.Drawing.Size(184, 166);
            this.RenderControl.TabIndex = 0;
            this.RenderControl.VSync = false;
            this.RenderControl.KeyDown += new System.Windows.Forms.KeyEventHandler(this.RenderControl_KeyDown);
            this.RenderControl.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.RenderControl_KeyPress);
            this.RenderControl.MouseClick += new System.Windows.Forms.MouseEventHandler(this.RenderControl_MouseClick);
            this.RenderControl.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.RenderControl_MouseDoubleClick);
            this.RenderControl.MouseDown += new System.Windows.Forms.MouseEventHandler(this.RenderControl_MouseDown);
            this.RenderControl.MouseEnter += new System.EventHandler(this.RenderControl_MouseEnter);
            this.RenderControl.MouseLeave += new System.EventHandler(this.RenderControl_MouseLeave);
            this.RenderControl.MouseMove += new System.Windows.Forms.MouseEventHandler(this.RenderControl_MouseMove);
            this.RenderControl.MouseUp += new System.Windows.Forms.MouseEventHandler(this.RenderControl_MouseUp);
            // 
            // FrameTimer
            // 
            this.FrameTimer.Interval = 20;
            this.FrameTimer.Tick += new System.EventHandler(this.FrameTimer_Tick);
            // 
            // FreeserfForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(184, 166);
            this.Controls.Add(this.RenderControl);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "FreeserfForm";
            this.Text = "Freeserf.net";
            this.Load += new System.EventHandler(this.FreeserfForm_Load);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.FreeserfForm_KeyDown);
            this.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.FreeserfForm_KeyPress);
            this.ResumeLayout(false);

        }

        #endregion

        private OpenTK.GLControl RenderControl;
        private System.Windows.Forms.Timer FrameTimer;
    }
}


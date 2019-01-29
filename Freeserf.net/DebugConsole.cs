using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Freeserf
{
    public partial class DebugConsole : Form
    {
        class RealTimeStream : MemoryStream
        {
            public event EventHandler Flushed;

            public override void Flush()
            {
                base.Flush();

                Flushed?.Invoke(this, EventArgs.Empty);
            }
        }

        RealTimeStream stream = new RealTimeStream();
        StreamReader streamReader = null;
        long readPosition = 0;
        bool logging = true;

        public DebugConsole()
        {
            InitializeComponent();

            stream.Flushed += Stream_Flushed;
        }

        private void Stream_Flushed(object sender, EventArgs e)
        {
            stream.Position = readPosition;
            TextBoxConsole.AppendText(streamReader.ReadToEnd());
            readPosition = stream.Length;

            if (readPosition > long.MaxValue / 2)
            {
                stream.Flushed -= Stream_Flushed;
                streamReader?.Dispose();

                stream = new RealTimeStream();
                streamReader = new StreamReader(stream, Encoding.UTF8);
                readPosition = 0;

                Log.SetStream(stream);                
            }
        }

        public void AttachLog()
        {
            Log.SetStream(stream);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            Log.SetStream(null);
            streamReader?.Dispose();
        }

        private void DebugConsole_Load(object sender, EventArgs e)
        {
            streamReader = new StreamReader(stream, Encoding.UTF8);
        }

        private void ButtonClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void ButtonStop_Click(object sender, EventArgs e)
        {
            logging = !logging;

            if (logging)
            {
                ButtonStop.Text = "Stop";
                Log.SetStream(stream);
            }
            else
            {
                ButtonStop.Text = "Resume";
                Log.SetStream(null);
            }
        }
    }
}

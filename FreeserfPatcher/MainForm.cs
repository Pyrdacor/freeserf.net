using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace FreeserfPatcher
{
    public partial class MainForm : Form
    {
        static readonly string DestinationPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        static readonly string UpdateUri = "https://github.com/Pyrdacor/freeserf.net/raw/master/updates/Windows/";
        readonly BackgroundWorker patchWorker = new BackgroundWorker();
        readonly string freeserfExePath = "";
        readonly string currentVersion = "";
        readonly string newVersion = "";
        readonly List<string> freeserfArgs = new List<string>();
        bool close = false;

        public MainForm(string[] args)
        {
            if (args.Length < 3)
            {
                close = true;
                InitializeComponent();
                return;
            }

            freeserfExePath = args[0];
            currentVersion = args[1];
            newVersion = args[2];

            for (int i = 3; i < args.Length; ++i)
                freeserfArgs.Add(args[i]);

            InitializeComponent();
        }

        void ButtonPatch_Click(object sender, EventArgs e)
        {
            patchWorker.WorkerReportsProgress = true;
            patchWorker.WorkerSupportsCancellation = false;
            patchWorker.DoWork += PatchWorker_DoWork;
            patchWorker.RunWorkerCompleted += PatchWorker_RunWorkerCompleted;
            patchWorker.ProgressChanged += PatchWorker_ProgressChanged;

            ButtonQuit.Enabled = false;
            ButtonSkip.Enabled = false;
            ButtonPatch.Enabled = false;
            ProgressBarPatch.Enabled = true;
            LabelProgress.Visible = true;

            patchWorker.RunWorkerAsync();
        }

        void PatchWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ProgressBarPatch.Value = e.ProgressPercentage;
        }

        void PatchWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            RunFreeserf();
            Close();
        }

        string RetrieveTextFile(string uri, int bufferSize)
        {
            try
            {
                var data = RetrieveFile(uri, bufferSize);

                if (data == null)
                    return null;

                return Encoding.UTF8.GetString(data);
            }
            catch (Exception ex)
            {
                // TODO: update failed -> invalid data (e.g. not UTF8)
                return null;
            }
        }

        byte[] RetrieveFile(string uri, int bufferSize)
        {
            // TODO: connection timeout / async
            HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(uri);
            httpRequest.Method = WebRequestMethods.Http.Get;

            HttpWebResponse httpResponse = (HttpWebResponse)httpRequest.GetResponse();
            var responseStream = httpResponse.GetResponseStream();

            byte[] buffer = new byte[bufferSize];
            List<byte> data = new List<byte>();

            while (true)
            {
                int length = responseStream.Read(buffer, 0, buffer.Length);

                if (length == 0)
                    break;

                data.AddRange(buffer.Take(length));
            }

            if (data.Count == 0)
            {
                // TODO: update failed -> missing data
                return null;
            }

            return data.ToArray();
        }

        void PatchWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            SetProgressLabel("Retrieving file list ...");

            string fileList = UpdateUri + newVersion + "/files.txt";
            string fileListContent = RetrieveTextFile(fileList, 128);

            if (fileListContent == null)
            {
                // TODO: update failed -> missing file list
                return;
            }

            patchWorker.ReportProgress(5);

            string[] entries = fileListContent.Replace("\r\n", "\n").Replace("\r", "").Split('\n');
            int progress = 5;
            int step = Math.Max(1, 95 / entries.Length);

            foreach (var entry in entries)
            {
                UpdateFile(UpdateUri + newVersion + "/" + entry);

                progress = Math.Min(100, progress + step);

                patchWorker.ReportProgress(progress);
            }
        }

        void UpdateFile(string filename)
        {
            try
            {
                SetProgressLabel("Retrieving \"" + Path.GetFileName(filename) + "\" ...");

                var fileData = RetrieveFile(filename, 2048);

                string localPath = Path.Combine(DestinationPath, Path.GetFileName(filename));

                try
                {
                    File.WriteAllBytes(localPath, fileData);
                }
                catch
                {
                    // TODO: error copying file
                }
            }
            catch
            {
                // TODO: error retrieving file or filename is corrupt, etc
            }
        }

        void SetProgressLabel(string label)
        {
            if (InvokeRequired)
            {
                Invoke(new MethodInvoker(delegate
                {
                    SetProgressLabel(label);
                }));

                return;
            }

            LabelProgress.Text = label;
        }

        void ButtonSkip_Click(object sender, EventArgs e)
        {
            RunFreeserf();
            Close();
        }

        void ButtonQuit_Click(object sender, EventArgs e)
        {
            Close();
        }

        void MainForm_Load(object sender, EventArgs e)
        {
            if (close)
            {
                MessageBox.Show("Please don't run this program directly." + Environment.NewLine + "It is called by Freeserf.net automatically.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Close();
                return;
            }

            LabelText.Text = "A new patch is available." + Environment.NewLine +
                Environment.NewLine +
                "Current version:   " + currentVersion + Environment.NewLine +
                "Available version: " + newVersion + Environment.NewLine +
                Environment.NewLine +
                "Do you want to upgrade?";

            ProgressBarPatch.Maximum = 100;
            ProgressBarPatch.Value = 0;
        }

        void RunFreeserf()
        {
            if (!freeserfArgs.Contains("--no-updates"))
                freeserfArgs.Add("--no-updates");

            Process.Start(freeserfExePath, string.Join(" ", freeserfArgs.ToArray()));
        }
    }
}

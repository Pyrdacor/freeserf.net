/*
 * CrashHandlerForm.cs - Crash Handler Form
 *
 * Copyright (C) 2019 Robert Schneckenhaus <robert.schneckenhaus@web.de>
 *
 * This file is part of freeserf.net. freeserf.net is based on freeserf.
 *
 * freeserf.net is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * freeserf.net is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with freeserf.net. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Diagnostics;
using System.Text;
using System.Web;
using System.Windows.Forms;

namespace Freeserf
{
    public partial class CrashHandlerForm : Form, UI.ICrashHandler
    {
        public CrashHandlerForm()
        {
            InitializeComponent();
        }

        string mailContent = "";
        Game game = null;
        public string ReportEmail { get; set; } = "robert.schneckenhaus@web.de";
        public string ReportSubject { get; set; } = "Crash Report";

        public UI.CrashReaction RaiseException(Exception exception)
        {
            if (exception == null)
            {
                try
                {
                    Log.Debug.Write(ErrorSystemType.Application, "Null exception raised.");
                }
                catch
                {
                    // ignore
                }

                return UI.CrashReaction.Close;
            }

            game = (exception is ExceptionFreeserf) ? (exception as ExceptionFreeserf).Game : null;

            ButtonSave.Visible = game != null;
            ButtonSendReport.Visible = true;

            GenerateMailContent(exception);

            if (string.IsNullOrWhiteSpace(exception.StackTrace))
            {
                SetText(exception.Message);
            }
            else
            {
                SetText(exception.Message + Environment.NewLine + Environment.NewLine + new string('=', 10) + " Stack Trace: " + new string('=', 10) +
                    Environment.NewLine + Environment.NewLine + exception.StackTrace);
            }

            var result = ShowDialog();

            if (result == DialogResult.Retry)
            {
                return UI.CrashReaction.Restart;
            }
            else
            {
                return UI.CrashReaction.Close;
            }
        }

        void GenerateMailContent(Exception exception)
        {
            mailContent = exception.Message;

            if (exception is ExceptionFreeserf)
            {
                var ex = exception as ExceptionFreeserf;

                mailContent += "\n\nFile: " + ex.SourceFile ?? "";
                mailContent += "\nLine: " + ex.SourceLineNumber;

                if (ex.Game != null)
                {
                    // TODO
                }
            }
        }

        void SetText(string text)
        {
            TextMessage.Text = text;
        }

        void AppendText(string text)
        {
            TextMessage.AppendText(text);
        }

        private void ButtonSendReport_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start($"mailto:{ReportEmail}?subject={Uri.EscapeDataString(ReportSubject)}&body={Uri.EscapeDataString(mailContent)}");
            }
            catch
            {
                MessageBox.Show(this, "Error sending email.", "Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            ButtonSendReport.Visible = false;
        }

        private void ButtonSave_Click(object sender, EventArgs e)
        {
            if (GameStore.Instance != null && game != null)
            {
                bool saved = false;
                string path = "";

                try
                {
                    saved = GameStore.Instance.QuickSave("crash", game, out path);
                }
                catch
                {
                    saved = false;
                }

                ButtonSave.Visible = false;

                if (!saved)
                    AppendText(Environment.NewLine + Environment.NewLine + "Unfortunately game saving failed.");
                else
                    AppendText(Environment.NewLine + Environment.NewLine + $"Game saved at \"{path}\".");
            }
        }
    }
}

/*
 * VoteBox.cs - Box for multiplayer votes
 *
 * Copyright (C) 2026  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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
using Freeserf.Localization;

namespace Freeserf.UI
{
    using Data = Data.Data;

    /// <summary>
    /// State of a vote on this participant.
    /// </summary>
    internal class VoteEntry
    {
        public uint VoteId;
        public uint Requester;
        public VoteType Type;
        public int Parameter;
        public bool OwnRequest;
        public DateTime Deadline;
        public bool Answered;
        public VoteResult? Result;
        public DateTime FinishTime = DateTime.MaxValue;

        public bool CanAnswer => !OwnRequest && !Answered && Result == null;
    }

    /// <summary>
    /// Shows a vote of a multiplayer game. The other players can agree or decline.
    /// The requester sees the state of its own request. At the end the result is shown.
    /// </summary>
    internal class VoteBox : Box
    {
        readonly Interface interf;
        readonly TextField[] textLines = new TextField[3];
        readonly TextField statusText;
        readonly TextField yesText;
        readonly TextField noText;
        readonly Icon playerFace;
        readonly Render.IColoredRect playerFaceBackground;

        public VoteBox(Interface interf)
            : base
            (
                interf,
                BackgroundPattern.CreateNotificationBoxBackground(interf.RenderView.SpriteFactory),
                Border.CreateNotificationBoxBorder(interf.RenderView.SpriteFactory)
            )
        {
            this.interf = interf;

            byte iconLayer = 2;

            playerFace = new Icon(interf, 32, 64, Data.Resource.Icon, 281u, iconLayer);
            AddChild(playerFace, 56, 16);

            playerFaceBackground = interf.RenderView.ColoredRectFactory.Create(48, 72, Render.Color.Transparent, 1);
            playerFaceBackground.Layer = Layer;

            for (int i = 0; i < textLines.Length; ++i)
            {
                textLines[i] = new TextField(interf, 3);
                AddChild(textLines[i], 8, 92 + i * MessageText.LineHeight);
            }

            statusText = new TextField(interf, 3);
            AddChild(statusText, 8, 124);

            yesText = new TextField(interf, 3);
            AddChild(yesText, 0, 0);

            noText = new TextField(interf, 3);
            AddChild(noText, 0, 0);
            UpdateAnswerLayout();
        }

        public VoteEntry Entry { get; private set; }

        /// <summary>
        /// Raised when the player answered the vote (true = yes).
        /// </summary>
        public event Action<VoteEntry, bool> Answered;

        public void Show(VoteEntry entry)
        {
            string[] lines = WrapMessage(entry.OwnRequest
                ? Vote.GetOwnRequest(entry.Type, entry.Parameter)
                : Vote.GetQuestion(entry.Requester, entry.Type, entry.Parameter));
            Entry = entry;
            Displayed = true;

            var player = interf.Game?.GetPlayer(entry.Requester);

            if (player != null)
            {
                var color = player.Color;
                playerFaceBackground.Color = new Render.Color() { R = color.Red, G = color.Green, B = color.Blue, A = 255 };
                playerFace.SetSpriteIndex(player.Face.GetGraphicIndex());
            }

            playerFace.Displayed = true;
            statusText.Displayed = true;
            SetText(lines);
            UpdateStatus(DateTime.UtcNow);
            SetRedraw();
        }

        /// <summary>
        /// Updates the buttons and the status text (e.g. the remaining time). Should be called regularly.
        /// </summary>
        public void UpdateStatus(DateTime now)
        {
            if (Entry == null)
                return;

            bool canAnswer = Entry.CanAnswer;
            yesText.Displayed = canAnswer && Displayed;
            noText.Displayed = canAnswer && Displayed;

            string text = VoteText.Status(Entry.Result, canAnswer, Entry.Deadline, now);

            if (statusText.Text != text)
            {
                statusText.Text = text;
                SetRedraw();
            }
        }

        void Answer(bool yes)
        {
            if (Entry == null || !Entry.CanAnswer)
                return;

            Entry.Answered = true;
            PlaySound(yes ? Freeserf.Audio.Audio.TypeSfx.Accepted : Freeserf.Audio.Audio.TypeSfx.NotAccepted);
            UpdateStatus(DateTime.UtcNow);

            Answered?.Invoke(Entry, yes);
        }

        internal static string[] WrapMessage(string text) =>
            MessageText.Wrap(text, MessageText.Columns, 3, "vote question");

        internal static ChoiceText[] GetAnswerLayout() =>
            ChoiceText.Layout(138, Strings.Vote_Answer_Yes, Strings.Vote_Answer_No);

        internal static bool? GetAnswerAt(int x, int y) =>
            ChoiceText.HitTest(GetAnswerLayout(), x, y) switch
            {
                0 => true,
                1 => false,
                _ => null
            };

        void UpdateAnswerLayout()
        {
            var choices = GetAnswerLayout();
            SetAnswerLayout(yesText, choices[0]);
            SetAnswerLayout(noText, choices[1]);
        }

        static void SetAnswerLayout(TextField text, ChoiceText choice)
        {
            text.Text = choice.Text;
            text.MoveTo(choice.X, choice.Y);
        }

        void SetText(string[] lines)
        {
            for (int i = 0; i < textLines.Length; ++i)
            {
                textLines[i].Text = i < lines.Length ? lines[i] : "";
                textLines[i].Displayed = i < lines.Length;
            }
        }

        internal override void RefreshLanguage()
        {
            UpdateAnswerLayout();
            if (Entry != null && Displayed)
            {
                SetText(WrapMessage(Entry.OwnRequest
                    ? Vote.GetOwnRequest(Entry.Type, Entry.Parameter)
                    : Vote.GetQuestion(Entry.Requester, Entry.Type, Entry.Parameter)));
                UpdateStatus(DateTime.UtcNow);
            }
            base.RefreshLanguage();
        }

        protected override void InternalDraw()
        {
            base.InternalDraw();

            playerFaceBackground.X = TotalX + 48;
            playerFaceBackground.Y = TotalY + 12;
            playerFaceBackground.DisplayLayer = (byte)(BaseDisplayLayer + 2);
            playerFaceBackground.Visible = Displayed;
        }

        protected override void InternalHide()
        {
            base.InternalHide();

            playerFaceBackground.Visible = false;
        }

        protected override bool HandleClickLeft(int x, int y, bool delayed)
        {
            if (!delayed && Displayed && Entry?.CanAnswer == true)
            {
                bool? answer = GetAnswerAt(x - TotalX, y - TotalY);
                if (answer.HasValue)
                    Answer(answer.Value);
            }
            // Clicks into the box must not reach the map.
            return true;
        }
    }
}

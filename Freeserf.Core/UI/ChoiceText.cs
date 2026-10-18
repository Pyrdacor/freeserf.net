using System;
using System.Linq;

namespace Freeserf.UI
{
    internal readonly record struct ChoiceText(string Text, int X, int Y, Rect Target)
    {
        internal static ChoiceText[] Layout(int y, params string[] labels)
        {
            if (labels == null || labels.Length < 2 || labels.Length > 3 ||
                labels.Any(label => string.IsNullOrWhiteSpace(label) || label.Any(char.IsControl)))
                MessageText.ReportLayoutError("choice row", "requires two or three nonempty single-line labels");

            int[] widths = labels.Select(label => Math.Max(24, label.Length * 8 + 8)).ToArray();
            int gap = (128 - widths.Sum()) / (labels.Length + 1);
            if (gap < 4)
                MessageText.ReportLayoutError("choice row", "cannot fit labels with padding and four-pixel gaps");

            var choices = new ChoiceText[labels.Length];
            int x = 8 + gap;
            for (int i = 0; i < labels.Length; ++i)
            {
                choices[i] = new ChoiceText(labels[i], x + (widths[i] - labels[i].Length * 8) / 2, y,
                    new Rect(x, y - 2, widths[i], 14));
                x += widths[i] + gap;
            }
            return choices;
        }

        internal static int HitTest(ChoiceText[] choices, int x, int y) =>
            Array.FindIndex(choices, choice => x >= choice.Target.Left && x < choice.Target.Right &&
                y >= choice.Target.Top && y < choice.Target.Bottom);
    }
}

using System;
using System.Collections.Generic;

namespace TerrariaEncyclopedia.Runtime.UI.Tooltips
{
    public static class TextLayout
    {
        public static IReadOnlyList<string> Wrap(
            string text,
            int maxWidth,
            Func<string, int> measure)
        {
            if (measure == null)
            {
                throw new ArgumentNullException(nameof(measure));
            }

            var lines = new List<string>();
            if (string.IsNullOrEmpty(text))
            {
                return lines;
            }

            if (maxWidth <= 0)
            {
                lines.Add(string.Empty);
                return lines;
            }

            string normalised = text.Replace("\r\n", "\n").Replace('\r', '\n');
            foreach (string paragraph in normalised.Split('\n'))
            {
                WrapParagraph(paragraph, maxWidth, measure, lines);
            }

            return lines;
        }

        public static string Truncate(string text, int maxWidth, Func<string, int> measure)
        {
            text = text ?? string.Empty;
            if (text.Length == 0 || maxWidth <= 0 || measure(text) <= maxWidth)
            {
                return maxWidth <= 0 ? string.Empty : text;
            }

            const string ellipsis = "...";
            int availableWidth = maxWidth - measure(ellipsis);
            if (availableWidth <= 0)
            {
                return string.Empty;
            }

            int low = 0;
            int high = text.Length;
            while (low < high)
            {
                int middle = (low + high + 1) / 2;
                if (measure(text.Substring(0, middle)) <= availableWidth)
                {
                    low = middle;
                }
                else
                {
                    high = middle - 1;
                }
            }

            return low == 0 ? string.Empty : text.Substring(0, low) + ellipsis;
        }

        public static string VisibleTail(string text, int maxWidth, Func<string, int> measure)
        {
            text = text ?? string.Empty;
            if (text.Length == 0 || maxWidth <= 0 || measure(text) <= maxWidth)
            {
                return maxWidth <= 0 ? string.Empty : text;
            }

            int low = 0;
            int high = text.Length;
            while (low < high)
            {
                int middle = (low + high) / 2;
                if (measure(text.Substring(middle)) <= maxWidth)
                {
                    high = middle;
                }
                else
                {
                    low = middle + 1;
                }
            }

            return text.Substring(low);
        }

        private static void WrapParagraph(
            string paragraph,
            int maxWidth,
            Func<string, int> measure,
            List<string> lines)
        {
            if (paragraph.Length == 0)
            {
                lines.Add(string.Empty);
                return;
            }

            string remaining = paragraph;
            while (measure(remaining) > maxWidth)
            {
                int breakAt = FindBreak(remaining, maxWidth, measure);
                lines.Add(remaining.Substring(0, breakAt).TrimEnd());
                remaining = remaining.Substring(breakAt).TrimStart();
                if (remaining.Length == 0)
                {
                    return;
                }
            }

            lines.Add(remaining);
        }

        private static int FindBreak(string text, int maxWidth, Func<string, int> measure)
        {
            int low = 1;
            int high = text.Length;
            while (low < high)
            {
                int middle = (low + high + 1) / 2;
                if (measure(text.Substring(0, middle)) <= maxWidth)
                {
                    low = middle;
                }
                else
                {
                    high = middle - 1;
                }
            }

            int fitting = Math.Max(1, low);
            for (int index = fitting; index > 0; index--)
            {
                if (char.IsWhiteSpace(text[index - 1]))
                {
                    return index;
                }
            }

            return fitting;
        }
    }
}

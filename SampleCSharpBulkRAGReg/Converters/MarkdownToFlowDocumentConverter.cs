using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;

namespace SampleCSharpBulkRAGReg.Converters
{
    public class MarkdownToFlowDocumentConverter : IValueConverter
    {
        // Simple regexes for basic markdown elements
        private static readonly Regex LinkRegex = new Regex(@"\[([^\]]+)\]\(([^)]+)\)", RegexOptions.Compiled);
        private static readonly Regex BoldRegex = new Regex(@"\*\*(.+?)\*\*", RegexOptions.Compiled);
        private static readonly Regex ItalicRegex = new Regex(@"\*(.+?)\*", RegexOptions.Compiled);
        private static readonly Regex CodeRegex = new Regex(@"`(.+?)`", RegexOptions.Compiled);

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var text = value as string;
            var doc = new FlowDocument();
            if (string.IsNullOrEmpty(text)) return doc;

            // Split into paragraphs on blank lines
            var paragraphs = Regex.Split(text.Trim(), @"\r?\n\s*\r?\n");
            foreach (var paraText in paragraphs)
            {
                var p = new Paragraph() {
                    Margin = new Thickness(0, 0, 0, 0), 
                    FontFamily = System.Windows.SystemFonts.MessageFontFamily,
                    FontSize = System.Windows.SystemFonts.MessageFontSize
                };
                AppendInlines(p.Inlines, paraText);
                doc.Blocks.Add(p);
            }

            return doc;
        }

        private void AppendInlines(InlineCollection inlines, string text)
        {
            int pos = 0;
            // Find earliest match among link/bold/italic/code
            while (pos < text.Length)
            {
                var match = FindFirstMatch(text, pos, out int kind);
                if (match == null)
                {
                    // remaining literal
                    inlines.Add(new Run(text.Substring(pos)));
                    break;
                }

                if (match.Index > pos)
                {
                    inlines.Add(new Run(text.Substring(pos, match.Index - pos)));
                }

                switch (kind)
                {
                    case 0: // link
                        var linkText = match.Groups[1].Value;
                        var url = match.Groups[2].Value;
                        var hl = new Hyperlink(new Run(linkText)) { NavigateUri = new Uri(url, UriKind.RelativeOrAbsolute) };
                        hl.RequestNavigate += (s, e) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
                        inlines.Add(hl);
                        break;
                    case 1: // bold
                        inlines.Add(new Bold(new Run(match.Groups[1].Value)));
                        break;
                    case 2: // italic
                        inlines.Add(new Italic(new Run(match.Groups[1].Value)));
                        break;
                    case 3: // code
                        var run = new Run(match.Groups[1].Value) { FontFamily = new FontFamily("Consolas"), Background = Brushes.LightGray };
                        inlines.Add(run);
                        break;
                }

                pos = match.Index + match.Length;
            }
        }

        // Return match and kind: 0=link,1=bold,2=italic,3=code
        private Match FindFirstMatch(string text, int start, out int kind)
        {
            Match best = null;
            kind = -1;

            var mLink = LinkRegex.Match(text, start);
            if (mLink.Success) { best = mLink; kind = 0; }

            var mBold = BoldRegex.Match(text, start);
            if (mBold.Success && (best == null || mBold.Index < best.Index)) { best = mBold; kind = 1; }

            var mItalic = ItalicRegex.Match(text, start);
            if (mItalic.Success && (best == null || mItalic.Index < best.Index)) { best = mItalic; kind = 2; }

            var mCode = CodeRegex.Match(text, start);
            if (mCode.Success && (best == null || mCode.Index < best.Index)) { best = mCode; kind = 3; }

            return best;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
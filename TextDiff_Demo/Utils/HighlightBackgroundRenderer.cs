using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Rendering;
using System.Collections.Generic;
using System.Linq;

namespace TextDiff_Demo.Utils
{
    public class HighlightBackgroundRenderer : IBackgroundRenderer
    {
        public KnownLayer Layer => KnownLayer.Background;

        private readonly TextEditor _editor;
        private readonly List<int> _linesToHighlight;
        private readonly Dictionary<int, List<(int Start, int Length)>> _lineRangesToHighlight;
        private readonly IBrush _lineBrush;
        private readonly IBrush _rangeBrush;

        public HighlightBackgroundRenderer(TextEditor editor, IEnumerable<int> linesToHighlight, Dictionary<int, List<(int Start, int Length)>> lineRangesToHighlight, IBrush lineBrush, IBrush rangeBrush)
        {
            _editor = editor;
            _linesToHighlight = linesToHighlight.ToList();
            _lineRangesToHighlight = lineRangesToHighlight;
            _lineBrush = lineBrush;
            _rangeBrush = rangeBrush;
        }

        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            if (!textView.VisualLinesValid) return;

            foreach (var line in textView.VisualLines)
            {
                // 高亮整行背景
                if (_linesToHighlight.Contains(line.FirstDocumentLine.LineNumber))
                {
                    var rect = BackgroundGeometryBuilder.GetRectsFromVisualSegment(textView, line, 0, line.VisualLength).First();
                    drawingContext.DrawRectangle(_lineBrush, null, rect);
                }

                // 高亮行中的特定范围
                if (_lineRangesToHighlight.TryGetValue(line.FirstDocumentLine.LineNumber, out var ranges))
                {
                    foreach (var (start, length) in ranges)
                    {
                        var rects = BackgroundGeometryBuilder.GetRectsFromVisualSegment(textView, line, start, length);
                        foreach (var rect in rects)
                        {
                            drawingContext.DrawRectangle(_rangeBrush, null, rect);
                        }
                    }
                }
            }
        }
    }
}

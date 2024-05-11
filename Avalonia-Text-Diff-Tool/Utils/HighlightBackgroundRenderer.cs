using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Rendering;

namespace Avalonia_Text_Diff_Tool.Utils;

public class HighlightBackgroundRenderer : IBackgroundRenderer
{
    private readonly TextEditor _editor;
    private readonly Dictionary<int, List<(int Start, int Length)>> _lineRangesToHighlight;
    private readonly Dictionary<int, IBrush> _linesToHighlight;
    private readonly IBrush _rangeBrush;

    public HighlightBackgroundRenderer(TextEditor editor,
        IEnumerable<(int LineNumber, IBrush LineBrush)> linesToHighlight,
        Dictionary<int, List<(int Start, int Length)>> lineRangesToHighlight, IBrush rangeBrush)
    {
        _editor = editor;
        _linesToHighlight = linesToHighlight.ToDictionary(tuple => tuple.LineNumber, tuple => tuple.LineBrush);
        _lineRangesToHighlight = lineRangesToHighlight;
        _rangeBrush = rangeBrush;
    }

    public KnownLayer Layer => KnownLayer.Background;

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (!textView.VisualLinesValid) return;

        foreach (var line in textView.VisualLines)
        {
            // 高亮整行背景
            if (_linesToHighlight.TryGetValue(line.FirstDocumentLine.LineNumber, out var lineBrush))
            {
                var rect = BackgroundGeometryBuilder.GetRectsFromVisualSegment(textView, line, 0, line.VisualLength)
                    .First();
                drawingContext.DrawRectangle(lineBrush, null, rect);
            }

            // 高亮行中的特定范围
            if (_lineRangesToHighlight.TryGetValue(line.FirstDocumentLine.LineNumber, out var ranges))
                foreach (var (start, length) in ranges)
                {
                    var rects = BackgroundGeometryBuilder.GetRectsFromVisualSegment(textView, line, start,
                        start + length);
                    foreach (var rect in rects) drawingContext.DrawRectangle(_rangeBrush, null, rect);
                }
        }
    }
}
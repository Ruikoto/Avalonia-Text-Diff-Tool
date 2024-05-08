using System.Linq;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Rendering;

namespace TextDiff_Demo.Utils;

public class HighlightBackgroundRenderer : IBackgroundRenderer
{
    public KnownLayer Layer => KnownLayer.Background;

    private TextEditor _editor;
    private readonly int _lineNumber;
    private readonly IBrush _lineBrush;

    public HighlightBackgroundRenderer(TextEditor editor, int lineNumber, IBrush lineBrush)
    {
        _editor = editor;
        _lineNumber = lineNumber;
        _lineBrush = lineBrush;
    }

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (!textView.VisualLinesValid) return;
        foreach (var line in textView.VisualLines)
        {
            if (line.FirstDocumentLine.LineNumber != _lineNumber) continue;
            var rect = BackgroundGeometryBuilder.GetRectsFromVisualSegment(textView, line, 0, line.VisualLength).First();
            drawingContext.DrawRectangle(_lineBrush, null, rect);
        }
    }
}
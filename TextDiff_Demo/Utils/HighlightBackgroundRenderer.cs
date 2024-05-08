using AvaloniaEdit.Rendering;
using Avalonia.Media;
using System;
using System.Linq;
using AvaloniaEdit;

public class HighlightBackgroundRenderer : IBackgroundRenderer
{
    public KnownLayer Layer => KnownLayer.Background;

    private TextEditor _editor;
    private int _lineNumber;
    private IBrush _lineBrush;

    public HighlightBackgroundRenderer(TextEditor editor, int lineNumber, IBrush lineBrush)
    {
        _editor = editor;
        _lineNumber = lineNumber;
        _lineBrush = lineBrush;
    }

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (textView.VisualLinesValid)
        {
            foreach (var line in textView.VisualLines)
            {
                if (line.FirstDocumentLine.LineNumber == _lineNumber)
                {
                    var rect = BackgroundGeometryBuilder.GetRectsFromVisualSegment(textView, line, 0, line.VisualLength).First();
                    drawingContext.DrawRectangle(_lineBrush, null, rect);
                }
            }
        }
    }
}
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using AvaloniaEdit;

namespace TextDiff_Demo.Behaviors;

public class SyncScrollBehavior
{
    private readonly TextEditor _leftTextEditor;
    private readonly TextEditor _rightTextEditor;

    private ScrollViewer? _leftScrollViewer;
    private ScrollViewer? _rightScrollViewer;

    private bool _isLeftScrolling;
    private bool _isRightScrolling;

    public SyncScrollBehavior(TextEditor leftTextEditor, TextEditor rightTextEditor)
    {
        _leftTextEditor = leftTextEditor;
        _rightTextEditor = rightTextEditor;

        _leftTextEditor.TextArea.TextView.ScrollOffsetChanged += LeftScrollChanged;
        _leftTextEditor.TextChanged += LeftScrollChanged;
        _rightTextEditor.TextArea.TextView.ScrollOffsetChanged += RightScrollChanged;
        _rightTextEditor.TextChanged += RightScrollChanged;
    }

    private void LeftScrollChanged(object? sender, System.EventArgs e)
    {
        if (_isRightScrolling) return;
        if (_leftScrollViewer == null || _rightScrollViewer == null)
        {
            GetScrollViewer();
            if (_leftScrollViewer == null || _rightScrollViewer == null) return;
        }

        _isLeftScrolling = true;
        var verticalOffset = _leftTextEditor.VerticalOffset;
        var horizontalOffset = _leftTextEditor.HorizontalOffset;
        _rightScrollViewer.Offset = new Vector(horizontalOffset, verticalOffset);

        _isLeftScrolling = false;
    }

    private void RightScrollChanged(object? sender, System.EventArgs e)
    {
        if (_isLeftScrolling) return;
        if (_leftScrollViewer == null || _rightScrollViewer == null)
        {
            GetScrollViewer();
            if (_leftScrollViewer == null || _rightScrollViewer == null) return;
        }

        _isRightScrolling = true;
        var verticalOffset = _rightTextEditor.VerticalOffset;
        var horizontalOffset = _rightTextEditor.HorizontalOffset;
        _leftScrollViewer.Offset = new Vector(horizontalOffset, verticalOffset);
        _isRightScrolling = false;
    }

    private void GetScrollViewer()
    {
        _leftScrollViewer = (ScrollViewer)typeof(TextEditor).GetProperty("ScrollViewer",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(_leftTextEditor)!;
        _rightScrollViewer = (ScrollViewer)typeof(TextEditor).GetProperty("ScrollViewer",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(_rightTextEditor)!;
    }
}
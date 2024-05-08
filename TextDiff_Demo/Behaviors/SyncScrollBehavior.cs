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
        _rightTextEditor.TextArea.TextView.ScrollOffsetChanged += RightScrollChanged;
    }

    private void LeftScrollChanged(object sender, System.EventArgs e)
    {
        if (_isRightScrolling) return;
        if (_leftScrollViewer == null || _rightScrollViewer == null)
        {
            GetScrollViewer();
            if (_leftScrollViewer == null || _rightScrollViewer == null) return;
        }

        _isLeftScrolling = true;

        var offset = _leftTextEditor.VerticalOffset;
        _rightScrollViewer.Offset = new Vector(0, offset);

        _isLeftScrolling = false;
    }

    private void RightScrollChanged(object sender, System.EventArgs e)
    {
        if (_isLeftScrolling) return;
        if (_leftScrollViewer == null || _rightScrollViewer == null)
        {
            GetScrollViewer();
            if (_leftScrollViewer == null || _rightScrollViewer == null) return;
        }

        _isRightScrolling = true;

        var offset = _rightTextEditor.VerticalOffset;
        _leftScrollViewer.Offset = new Vector(0, offset);

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
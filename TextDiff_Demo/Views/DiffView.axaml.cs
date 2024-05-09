using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using AvaloniaEdit;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using TextDiff_Demo.Utils;
using TextDiff_Demo.ViewModels;

namespace TextDiff_Demo.Views;

public partial class DiffView : UserControl
{
    public DiffView()
    {
        InitializeComponent();

        ViewModel = new DiffViewModel();
        DataContext = ViewModel;

        _lineHeight = NewerEditor.TextArea.TextView.DefaultLineHeight;

        OlderEditor.TextArea.TextView.ScrollOffsetChanged += OnLeftScrollChanged;
        NewerEditor.TextArea.TextView.ScrollOffsetChanged += RightScrollChanged;
        OlderEditor.TextChanged += OnLeftScrollChanged;
        NewerEditor.TextChanged += RightScrollChanged;
        OlderEditor.TextChanged += OnEdit;
        NewerEditor.TextChanged += OnEdit;
    }

    private DiffViewModel ViewModel { get; }

    private ScrollViewer? _leftScrollViewer;
    private ScrollViewer? _rightScrollViewer;

    private bool _isLeftScrolling;
    private bool _isRightScrolling;
    private readonly double _lineHeight;
    private SideBySideDiffModel? _diffResult;

    // 刷新按钮点击事件
    private void Refresh_OnClick(object? sender, RoutedEventArgs e)
    {
        Render(OlderEditor.Text, NewerEditor.Text);
    }

    // 编辑事件
    private void OnEdit(object? sender, EventArgs e)
    {
        Render(OlderEditor.Text, NewerEditor.Text);
    }

    // 渲染差异
    private void Render(string oldText, string newText, bool ignoreWhitespace = true, bool ignoreCase = false)
    {
        _diffResult = SideBySideDiffBuilder.Diff(oldText, newText, ignoreWhitespace, ignoreCase);

        if (_diffResult == null || (!_diffResult.NewText.HasDifferences && !_diffResult.OldText.HasDifferences))
        {
            OlderEditor.TextArea.TextView.BackgroundRenderers.Clear();
            NewerEditor.TextArea.TextView.BackgroundRenderers.Clear();
            return;
        }

        // 创建字典来存储需要高亮显示的行和范围
        var oldTextLinesToHighlight = new HashSet<int>();
        var oldTextRangesToHighlight = new Dictionary<int, List<(int Start, int Length)>>();

        var newTextLinesToHighlight = new HashSet<int>();
        var newTextRangesToHighlight = new Dictionary<int, List<(int Start, int Length)>>();

        // 处理旧文本差异
        foreach (var line in _diffResult.OldText.Lines.Where(line => line.Type != ChangeType.Unchanged))
        {
            if (!line.Position.HasValue) continue;
            oldTextLinesToHighlight.Add(line.Position.Value);

            var subPieceRanges = new List<(int Start, int Length)>();
            int currentPosition = 0;

            // 遍历子片段，收集需要高亮的范围
            foreach (var subPiece in line.SubPieces)
            {
                int startPosition = currentPosition;
                int length = subPiece.Text?.Length ?? 0;

                if (subPiece.Type != ChangeType.Unchanged)
                {
                    subPieceRanges.Add((startPosition, length));
                }

                // 更新当前位置
                currentPosition += length;
            }

            oldTextRangesToHighlight[line.Position.Value] = subPieceRanges;
        }

        // 处理新文本差异
        foreach (var line in _diffResult.NewText.Lines.Where(line => line.Type != ChangeType.Unchanged))
        {
            if (!line.Position.HasValue) continue;
            newTextLinesToHighlight.Add(line.Position.Value);

            var subPieceRanges = new List<(int Start, int Length)>();
            int currentPosition = 0;

            // 遍历子片段，收集需要高亮的范围
            foreach (var subPiece in line.SubPieces)
            {
                int startPosition = currentPosition;
                int length = subPiece.Text?.Length ?? 0;

                if (subPiece.Type != ChangeType.Unchanged)
                {
                    subPieceRanges.Add((startPosition, length));
                }

                // 更新当前位置
                currentPosition += length;
            }

            newTextRangesToHighlight[line.Position.Value] = subPieceRanges;
        }

        // 定义背景颜色刷
        IBrush lineBrushRed = new SolidColorBrush(Color.Parse("#FFFFAACC"));
        IBrush lineBrushGreen = new SolidColorBrush(Color.Parse("#FFd1e3c9"));
        IBrush rangeBrushRed = new SolidColorBrush(Color.Parse("#FFcc88a3"));
        IBrush rangeBrushGreen = new SolidColorBrush(Color.Parse("#FF96c294"));

        // 清空原有的高亮渲染器
        OlderEditor.TextArea.TextView.BackgroundRenderers.Clear();
        OlderEditor.InvalidateVisual();
        NewerEditor.TextArea.TextView.BackgroundRenderers.Clear();
        NewerEditor.InvalidateVisual();

        // 为旧文本创建并添加高亮渲染器
        var oldHighlightRenderer = new HighlightBackgroundRenderer(OlderEditor, oldTextLinesToHighlight,
            oldTextRangesToHighlight, lineBrushRed, rangeBrushRed);
        OlderEditor.TextArea.TextView.BackgroundRenderers.Add(oldHighlightRenderer);

        // 为新文本创建并添加高亮渲染器
        var newHighlightRenderer = new HighlightBackgroundRenderer(NewerEditor, newTextLinesToHighlight,
            newTextRangesToHighlight, lineBrushGreen, rangeBrushGreen);
        NewerEditor.TextArea.TextView.BackgroundRenderers.Add(newHighlightRenderer);
    }

    private void OnLeftScrollChanged(object? sender, EventArgs e)
    {
        if (_isRightScrolling) return;
        if (_diffResult == null) return;
        if (_leftScrollViewer == null || _rightScrollViewer == null)
        {
            GetScrollViewer();
            if (_leftScrollViewer == null || _rightScrollViewer == null) return;
        }

        _isLeftScrolling = true;

        // 取得当前滚动位置
        var verticalOffset = OlderEditor.VerticalOffset;
        var horizontalOffset = OlderEditor.HorizontalOffset;

        // 计算行号
        var lineNumber = (int)(verticalOffset / _lineHeight) + 1;

        // 查找对应行号的新文本行号
        var leftActualLine = _diffResult.OldText.Lines.FindIndex(x => x.Position == lineNumber);
        if (leftActualLine == -1)
        {
            _isLeftScrolling = false;
            return;
        }

        var rightScrollLine = _diffResult.NewText.Lines[leftActualLine]?.Position;

        // 滚动到对应行号
        if (rightScrollLine.HasValue)
        {
            var rightOffset = (rightScrollLine.Value - 0) * _lineHeight;
            _rightScrollViewer.Offset = new Vector(horizontalOffset, rightOffset);
        }

        // 左侧对齐滚动
        // _leftScrollViewer.Offset = new Vector(horizontalOffset, (lineNumber - 1) * _lineHeight);

        _isLeftScrolling = false;
    }

    private void RightScrollChanged(object? sender, EventArgs e)
    {
        if (_isLeftScrolling) return;
        if (_diffResult == null) return;
        if (_leftScrollViewer == null || _rightScrollViewer == null)
        {
            GetScrollViewer();
            if (_leftScrollViewer == null || _rightScrollViewer == null) return;
        }

        _isRightScrolling = true;

        // 取得当前滚动位置
        var verticalOffset = NewerEditor.VerticalOffset;
        var horizontalOffset = NewerEditor.HorizontalOffset;

        // 计算行号
        var lineNumber = (int)(verticalOffset / _lineHeight) + 1;

        // 查找对应行号的旧文本行号
        var rightActualLine = _diffResult.NewText.Lines.FindIndex(x => x.Position == lineNumber);
        if (rightActualLine == -1)
        {
            _isRightScrolling = false;
            return;
        }

        var leftScrollLine = _diffResult.OldText.Lines[rightActualLine]?.Position;

        // 滚动到对应行号
        if (leftScrollLine.HasValue)
        {
            var leftOffset = (leftScrollLine.Value - 0) * _lineHeight;
            _leftScrollViewer.Offset = new Vector(horizontalOffset, leftOffset);
        }

        // 右侧对齐滚动
        // _rightScrollViewer.Offset = new Vector(horizontalOffset, (lineNumber - 0) * _lineHeight);

        _isRightScrolling = false;
    }

    private void GetScrollViewer()
    {
        _leftScrollViewer = (ScrollViewer)typeof(TextEditor).GetProperty("ScrollViewer",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(OlderEditor)!;
        _rightScrollViewer = (ScrollViewer)typeof(TextEditor).GetProperty("ScrollViewer",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(NewerEditor)!;
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using AvaloniaEdit;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using Avalonia_Text_Diff_Tool.Utils;
using Avalonia_Text_Diff_Tool.ViewModels;
using Avalonia.Threading;
using ChangeType = DiffPlex.DiffBuilder.Model.ChangeType;

namespace Avalonia_Text_Diff_Tool.Views;

public partial class DiffView : UserControl
{
    private readonly double _lineHeight;

    private readonly DiffViewModel _viewModel;
    private SideBySideDiffModel? _diffResult;
    private bool _isLeftScrolling;
    private bool _isRightScrolling;
    private bool _isReplacingText;
    private ScrollViewer? _leftScrollViewer;
    private ScrollViewer? _rightScrollViewer;

    private readonly IBrush _lineBrushRed = new SolidColorBrush(Color.Parse("#FFFFAACC"));
    private readonly IBrush _lineBrushGreen = new SolidColorBrush(Color.Parse("#FFd1e3c9"));
    private readonly IBrush _lineBrushGray = new SolidColorBrush(Color.Parse("#FFa4a4a4"));
    private readonly IBrush _rangeBrushRed = new SolidColorBrush(Color.Parse("#FFcc88a3"));
    private readonly IBrush _rangeBrushGreen = new SolidColorBrush(Color.Parse("#FF96c294"));


    public DiffView()
    {
        InitializeComponent();

        _viewModel = new DiffViewModel();
        DataContext = _viewModel;

        _lineHeight = NewerEditor.TextArea.TextView.DefaultLineHeight;

        OlderEditor.TextArea.TextView.ScrollOffsetChanged += OnLeftScrollChanged;
        NewerEditor.TextArea.TextView.ScrollOffsetChanged += RightScrollChanged;
        OlderEditor.TextChanged += OnLeftScrollChanged;
        NewerEditor.TextChanged += RightScrollChanged;
        OlderEditor.TextChanged += OnEdit;
        NewerEditor.TextChanged += OnEdit;
    }

    #region Render Diff

    // 刷新按钮点击事件
    private void Refresh_OnClick(object? sender, RoutedEventArgs e)
    {
        var olderText = Regex.Replace(OlderEditor.Text.Replace("\u200b\r\n", string.Empty), @"(\r?\n){2,}$", "\n");
        var newerText = Regex.Replace(NewerEditor.Text.Replace("\u200b\r\n", string.Empty), @"(\r?\n){2,}$", "\n");
        Render(olderText, newerText, false);
    }

    // 编辑事件
    private void OnEdit(object? sender, EventArgs e)
    {
        if (_isReplacingText) return;
        if (!_viewModel.RealTimeDiffering) return;
        var olderText = Regex.Replace(OlderEditor.Text.Replace("\u200b\r\n", string.Empty), @"(\r?\n){2,}$", "\n");
        var newerText = Regex.Replace(NewerEditor.Text.Replace("\u200b\r\n", string.Empty), @"(\r?\n){2,}$", "\n");
        Render(olderText, newerText, false);
    }

    // 渲染差异
    private void Render(string oldText, string newText, bool ignoreWhitespace = true, bool ignoreCase = false)
    {
        _diffResult = SideBySideDiffBuilder.Diff(oldText, newText, ignoreWhitespace, ignoreCase);

        if (_diffResult == null || (!_diffResult.NewText.HasDifferences && !_diffResult.OldText.HasDifferences))
        {
            OlderEditor.TextArea.TextView.BackgroundRenderers.Clear();
            NewerEditor.TextArea.TextView.BackgroundRenderers.Clear();
            OlderEditorScrollIndicatorCanvas.Children.Clear();
            NewerEditorScrollIndicatorCanvas.Children.Clear();
            return;
        }

        RenderTextDiff();
        RenderScrollIndicators();
    }

    private void RenderTextDiff()
    {
        // 创建字典来存储需要高亮显示的行和范围
        var oldTextLinesToHighlight = new HashSet<(int,IBrush)>();
        var oldTextRangesToHighlight = new Dictionary<int, List<(int Start, int Length)>>();

        var newTextLinesToHighlight = new HashSet<(int,IBrush)>();
        var newTextRangesToHighlight = new Dictionary<int, List<(int Start, int Length)>>();

        // 处理旧文本差异
        for (var i = 0; i < _diffResult!.OldText.Lines.Count; i++)
        {
            if (_diffResult!.OldText.Lines[i].Type == ChangeType.Unchanged) continue;
            var line = _diffResult!.OldText.Lines[i];

            oldTextLinesToHighlight.Add(line.Type is ChangeType.Imaginary or ChangeType.Deleted
                ? (i + 1, _lineBrushGray)
                : (i + 1, _lineBrushRed));


            var subPieceRanges = new List<(int Start, int Length)>();
            var currentPosition = 0;

            // 遍历子片段，收集需要高亮的范围
            foreach (var subPiece in line.SubPieces)
            {
                var startPosition = currentPosition;
                var length = subPiece.Text?.Length ?? 0;

                if (subPiece.Type != ChangeType.Unchanged) subPieceRanges.Add((startPosition, length));

                // 更新当前位置
                currentPosition += length;
            }

            oldTextRangesToHighlight[i+1] = subPieceRanges;
        }

        // 处理新文本差异
        for (var i = 0; i < _diffResult!.NewText.Lines.Count; i++)
        {
            if (_diffResult!.NewText.Lines[i].Type == ChangeType.Unchanged) continue;
            var line = _diffResult!.NewText.Lines[i];

            newTextLinesToHighlight.Add(line.Type is ChangeType.Imaginary
                ? (i + 1, _lineBrushGray)
                : (i + 1, _lineBrushGreen));

            var subPieceRanges = new List<(int Start, int Length)>();
            var currentPosition = 0;

            // 遍历子片段，收集需要高亮的范围
            foreach (var subPiece in line.SubPieces)
            {
                var startPosition = currentPosition;
                var length = subPiece.Text?.Length ?? 0;

                if (subPiece.Type != ChangeType.Unchanged) subPieceRanges.Add((startPosition, length));

                // 更新当前位置
                currentPosition += length;
            }

            newTextRangesToHighlight[i+1] = subPieceRanges;
        }

        // 清空原有的高亮渲染器
        OlderEditor.TextArea.TextView.BackgroundRenderers.Clear();
        NewerEditor.TextArea.TextView.BackgroundRenderers.Clear();

        // 为旧文本创建并添加高亮渲染器
        var oldHighlightRenderer = new HighlightBackgroundRenderer(OlderEditor, oldTextLinesToHighlight,
            oldTextRangesToHighlight,  _rangeBrushRed);
        OlderEditor.TextArea.TextView.BackgroundRenderers.Add(oldHighlightRenderer);

        // 为新文本创建并添加高亮渲染器
        var newHighlightRenderer = new HighlightBackgroundRenderer(NewerEditor, newTextLinesToHighlight,
            newTextRangesToHighlight,  _rangeBrushGreen);
        NewerEditor.TextArea.TextView.BackgroundRenderers.Add(newHighlightRenderer);

        // 保存光标位置
        var olderEditorCaretOffset = OlderEditor.CaretOffset;
        var newerEditorCaretOffset = NewerEditor.CaretOffset;

        // 替换空行
        Dispatcher.UIThread.Post(() =>
        {
            _isReplacingText = true;
            var olderSb = new StringBuilder();
            var newerSb = new StringBuilder();

            foreach (var line in _diffResult.OldText.Lines)
            {
                olderSb.AppendLine(line.Type == ChangeType.Imaginary ? "\u200b" : line.Text);
            }

            foreach (var line in _diffResult.NewText.Lines)
            {
                newerSb.AppendLine(line.Type == ChangeType.Imaginary ? "\u200b" : line.Text);
            }

            OlderEditor.Document.Text = olderSb.ToString();
            NewerEditor.Document.Text = newerSb.ToString();

            // 恢复光标位置
            try
            {
                OlderEditor.CaretOffset = olderEditorCaretOffset;
            }
            catch (ArgumentOutOfRangeException e)
            {
                // ignored
            }
            try
            {
                NewerEditor.CaretOffset = newerEditorCaretOffset;
            }
            catch (ArgumentOutOfRangeException e)
            {
                // ignored
            }

            _isReplacingText = false;
        }, DispatcherPriority.Background);
    }

    private void RenderScrollIndicators()
    {
        // 清空现有 Canvas
        OlderEditorScrollIndicatorCanvas.Children.Clear();
        NewerEditorScrollIndicatorCanvas.Children.Clear();

        // 获取文本框的总高度和每行的高度
        var olderEditorHeight = OlderEditor.Bounds.Height;
        var newerEditorHeight = NewerEditor.Bounds.Height;
        var olderLineHeight = OlderEditor.TextArea.TextView.DefaultLineHeight;
        var newerLineHeight = NewerEditor.TextArea.TextView.DefaultLineHeight;

        // 计算行高比例
        var olderLineHeightRatio = olderEditorHeight / OlderEditor.Document.LineCount;
        var newerLineHeightRatio = newerEditorHeight / NewerEditor.Document.LineCount;

        // 计算需要显示差异的行
        var olderDiffLines = new List<int>();
        var newerDiffLines = new List<int>();

        // TODO 改
        olderDiffLines.AddRange(
            (_diffResult?.OldText.Lines ?? Enumerable.Empty<DiffPiece>())
            .Where(line => line is { Position: not null, Type: not (ChangeType.Unchanged or ChangeType.Imaginary) })
            .Select(line => line.Position!.Value)
        );
        newerDiffLines.AddRange(
            (_diffResult?.NewText.Lines ?? Enumerable.Empty<DiffPiece>())
            .Where(line => line is { Position: not null, Type: not (ChangeType.Unchanged or ChangeType.Imaginary) })
            .Select(line => line.Position!.Value)
        );

        // 通过行号计算需要显示的矩形
        var olderRects = CalculateRectanglesFromLines(olderDiffLines, olderLineHeightRatio, olderLineHeight);
        var newerRects = CalculateRectanglesFromLines(newerDiffLines, newerLineHeightRatio, newerLineHeight);

        // 将矩形添加到 OlderEditor 的 Canvas 中
        foreach (var rect in olderRects)
            OlderEditorScrollIndicatorCanvas.Children.Add(new Border
            {
                Background = new SolidColorBrush(Color.Parse("#BBe64f8b")),
                Width = rect.Width,
                Height = rect.Height,
                Margin = new Thickness(rect.X, rect.Y, 0, 0)
            });

        // 将矩形添加到 NewerEditor 的 Canvas 中
        foreach (var rect in newerRects)
            NewerEditorScrollIndicatorCanvas.Children.Add(new Border
            {
                Background = new SolidColorBrush(Color.Parse("#BB50a74c")),
                Width = rect.Width,
                Height = rect.Height,
                Margin = new Thickness(rect.X, rect.Y, 0, 0)
            });
    }

    private static List<Rect> CalculateRectanglesFromLines(IEnumerable<int> lines, double lineHeightRatio,
        double maxHeight)
    {
        var rects = new List<Rect>();
        var lineGroups = GroupConsecutiveLines(lines);

        foreach (var group in lineGroups)
        {
            var startY = (group.First() - 1) * lineHeightRatio;
            var height = Math.Min(group.Count * lineHeightRatio, maxHeight);

            rects.Add(new Rect(0, startY, 20, height));
        }

        return rects;
    }

    private static List<List<int>> GroupConsecutiveLines(IEnumerable<int> lines)
    {
        var groupedLines = new List<List<int>>();
        List<int>? currentGroup = null;

        foreach (var line in lines.OrderBy(x => x))
            if (currentGroup == null || line != currentGroup.Last() + 1)
            {
                currentGroup = new List<int> { line };
                groupedLines.Add(currentGroup);
            }
            else
            {
                currentGroup.Add(line);
            }

        return groupedLines;
    }

    #endregion

    #region Scrolling

    private void OnLeftScrollChanged(object? sender, EventArgs e)
    {
        if (!_viewModel.SynchronousScrolling) return;
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

        // // 计算行号
        // var lineNumber = (int)(verticalOffset / _lineHeight) + 1;
        //
        // // 查找对应行号的新文本行号
        // var leftActualLine = _diffResult.OldText.Lines.FindIndex(x => x.Position == lineNumber);
        // if (leftActualLine == -1)
        // {
        //     _isLeftScrolling = false;
        //     return;
        // }
        //
        // var rightScrollLine = _diffResult.NewText.Lines[leftActualLine]?.Position;
        //
        // // 滚动到对应行号
        // if (rightScrollLine.HasValue)
        // {
        //     var rightOffset = (rightScrollLine.Value - 0) * _lineHeight;
        //     _rightScrollViewer.Offset = new Vector(horizontalOffset, rightOffset);
        // }

        // 左侧对齐滚动
        // _leftScrollViewer.Offset = new Vector(horizontalOffset, (lineNumber - 1) * _lineHeight);

        _rightScrollViewer.Offset = new Vector(horizontalOffset, verticalOffset);

        _isLeftScrolling = false;
    }

    private void RightScrollChanged(object? sender, EventArgs e)
    {
        if (!_viewModel.SynchronousScrolling) return;
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

        // // 计算行号
        // var lineNumber = (int)(verticalOffset / _lineHeight) + 1;
        //
        // // 查找对应行号的旧文本行号
        // var rightActualLine = _diffResult.NewText.Lines.FindIndex(x => x.Position == lineNumber);
        // if (rightActualLine == -1)
        // {
        //     _isRightScrolling = false;
        //     return;
        // }
        //
        // var leftScrollLine = _diffResult.OldText.Lines[rightActualLine]?.Position;
        //
        // // 滚动到对应行号
        // if (leftScrollLine.HasValue)
        // {
        //     var leftOffset = (leftScrollLine.Value - 0) * _lineHeight;
        //     _leftScrollViewer.Offset = new Vector(horizontalOffset, leftOffset);
        // }

        // 右侧对齐滚动
        // _rightScrollViewer.Offset = new Vector(horizontalOffset, (lineNumber - 0) * _lineHeight);

        _leftScrollViewer.Offset = new Vector(horizontalOffset, verticalOffset);

        _isRightScrolling = false;
    }

    private void GetScrollViewer()
    {
        _leftScrollViewer = (ScrollViewer)typeof(TextEditor).GetProperty("ScrollViewer",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(OlderEditor)!;
        _rightScrollViewer = (ScrollViewer)typeof(TextEditor).GetProperty("ScrollViewer",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(NewerEditor)!;
    }

    #endregion
}
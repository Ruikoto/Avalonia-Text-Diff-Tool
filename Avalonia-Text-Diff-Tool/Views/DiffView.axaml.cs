using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Avalonia;
using Avalonia_Text_Diff_Tool.Utils;
using Avalonia_Text_Diff_Tool.ViewModels;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using AvaloniaEdit;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using ChangeType = DiffPlex.DiffBuilder.Model.ChangeType;

namespace Avalonia_Text_Diff_Tool.Views;

public partial class DiffView : UserControl
{
    private const int ScrollIndicatorWidth = 20;

    private readonly IBrush _lineBrushGray = new SolidColorBrush(Color.Parse("#FFa4a4a4"));
    private readonly IBrush _lineBrushGreen = new SolidColorBrush(Color.Parse("#FFd1e3c9"));
    private readonly IBrush _lineBrushRed = new SolidColorBrush(Color.Parse("#FFffAAcc"));
    private readonly IBrush _lineBrushBlue = new SolidColorBrush(Color.Parse("#FFc9c9f2"));
    private readonly IBrush _rangeBrushGreen = new SolidColorBrush(Color.Parse("#FF96c294"));
    private readonly IBrush _rangeBrushBlue = new SolidColorBrush(Color.Parse("#FF9e9eBF"));
    private readonly double _lineHeight;
    private readonly DispatcherTimer _scrollIndicatorTimer;
    private readonly DiffViewModel _viewModel;

    private SideBySideDiffModel? _diffResult;
    private bool _isLeftScrolling;
    private bool _isReplacingText;
    private bool _isRightScrolling;
    private ScrollViewer? _leftScrollViewer;
    private ScrollViewer? _rightScrollViewer;

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

        _scrollIndicatorTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _scrollIndicatorTimer.Tick += ScrollIndicatorTimer_Tick;
    }

    private void ScrollIndicatorTimer_Tick(object? sender, EventArgs e)
    {
        _scrollIndicatorTimer.IsEnabled = false; // 使用 IsEnabled 而不是 Stop
        RenderScrollIndicators();
    }

    private void ScheduleRenderScrollIndicators()
    {
        // 如果计时器未启动，则启用计时器
        if (!_scrollIndicatorTimer.IsEnabled)
        {
            _scrollIndicatorTimer.IsEnabled = true;
        }
    }

    #region Render Diff

    // 刷新按钮点击事件
    private void Refresh_OnClick(object? sender, RoutedEventArgs e)
    {
        var olderText = OlderEditor.Text.Replace("\u200b\r\n", string.Empty);
        var newerText = NewerEditor.Text.Replace("\u200b\r\n", string.Empty);
        Render(olderText, newerText, false);
    }

    // 编辑事件
    private void OnEdit(object? sender, EventArgs e)
    {
        if (_isReplacingText) return;
        if (!_viewModel.RealTimeDiffering) return;
        var olderText = OlderEditor.Text.Replace("\u200b\r\n", string.Empty);
        var newerText = NewerEditor.Text.Replace("\u200b\r\n", string.Empty);
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
        ScheduleRenderScrollIndicators();
    }

    private void RenderTextDiff()
    {
        // 处理文本差异
        GenerateTextHighLight(_diffResult!.OldText.Lines, out var oldTextLinesToHighlight,
            out var oldTextRangesToHighlight);
        GenerateTextHighLight(_diffResult!.NewText.Lines, out var newTextLinesToHighlight,
            out var newTextRangesToHighlight);

        // 清空原有的高亮渲染器
        OlderEditor.TextArea.TextView.BackgroundRenderers.Clear();
        NewerEditor.TextArea.TextView.BackgroundRenderers.Clear();

        // 为旧文本创建并添加高亮渲染器
        var oldHighlightRenderer = new HighlightBackgroundRenderer(OlderEditor, oldTextLinesToHighlight,
            oldTextRangesToHighlight, _rangeBrushBlue);
        OlderEditor.TextArea.TextView.BackgroundRenderers.Add(oldHighlightRenderer);

        // 为新文本创建并添加高亮渲染器
        var newHighlightRenderer = new HighlightBackgroundRenderer(NewerEditor, newTextLinesToHighlight,
            newTextRangesToHighlight, _rangeBrushGreen);
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
                olderSb.AppendLine(line.Type == ChangeType.Imaginary ? "\u200b" : line.Text);

            foreach (var line in _diffResult.NewText.Lines)
                newerSb.AppendLine(line.Type == ChangeType.Imaginary ? "\u200b" : line.Text);

            OlderEditor.Text = olderSb.ToString().TrimEnd('\r', '\n');
            NewerEditor.Text = newerSb.ToString().TrimEnd('\r', '\n');

            // 恢复光标位置
            try
            {
                OlderEditor.CaretOffset = olderEditorCaretOffset;
            }
            catch (ArgumentOutOfRangeException)
            {
                // ignored
            }

            try
            {
                NewerEditor.CaretOffset = newerEditorCaretOffset;
            }
            catch (ArgumentOutOfRangeException)
            {
                // ignored
            }

            _isReplacingText = false;
        }, DispatcherPriority.Background);
    }

    private void GenerateTextHighLight(List<DiffPiece> lines,
        out HashSet<(int Index, IBrush Brush)> textLinesToHighlight,
        out Dictionary<int, List<(int Start, int Length)>> textRangesToHighlight)
    {
        textLinesToHighlight = [];
        textRangesToHighlight = [];

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];

            if (line.Type == ChangeType.Unchanged) continue;

            var brush = line.Type switch
            {
                ChangeType.Imaginary => _lineBrushGray,
                ChangeType.Deleted => _lineBrushRed,
                ChangeType.Inserted => _lineBrushGreen,
                ChangeType.Modified => _lineBrushBlue,
                _ => _lineBrushBlue
            };

            textLinesToHighlight.Add((i + 1, brush));

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

            textRangesToHighlight[i + 1] = subPieceRanges;
        }
    }

    private void RenderScrollIndicators()
    {
        // 清空现有 Canvas
        OlderEditorScrollIndicatorCanvas.Children.Clear();
        NewerEditorScrollIndicatorCanvas.Children.Clear();

        // 获取文本框的总高度和每行的高度
        var olderEditorHeight = OlderEditor.Bounds.Height;
        var newerEditorHeight = NewerEditor.Bounds.Height;

        // 获取总行数
        var olderTotalLines = OlderEditor.Document.LineCount;
        var newerTotalLines = NewerEditor.Document.LineCount;

        // 计算需要显示差异的行
        var olderDiffIndexes = GetDiffIndexes(_diffResult?.OldText.Lines);
        var newerDiffIndexes = GetDiffIndexes(_diffResult?.NewText.Lines);

        // 通过索引计算需要显示的矩形
        var olderRects =
            GenerateRectangles(olderDiffIndexes, olderEditorHeight, olderTotalLines, _lineHeight);
        var newerRects =
            GenerateRectangles(newerDiffIndexes, newerEditorHeight, newerTotalLines, _lineHeight);

        // 将矩形添加到 OlderEditor 的 Canvas 中
        foreach (var (x, y, width, height, brush) in olderRects)
            OlderEditorScrollIndicatorCanvas.Children.Add(new Border
            {
                Background = brush,
                Width = width,
                Height = height,
                Margin = new Thickness(x, y, 0, 0)
            });

        // 将矩形添加到 NewerEditor 的 Canvas 中
        foreach (var (x, y, width, height, brush) in newerRects)
            NewerEditorScrollIndicatorCanvas.Children.Add(new Border
            {
                Background = brush,
                Width = width,
                Height = height,
                Margin = new Thickness(x, y, 0, 0)
            });
    }


    private static List<(double X, double Y, double Width, double Height, IBrush Brush)> GenerateRectangles(
        IEnumerable<(int Index, IBrush Brush, int Count)> indexes, double totalHeight, int totalLines,
        double lineHeight)
    {
        var rects = new List<(double X, double Y, double Width, double Height, IBrush Brush)>();

        // 计算比例因子
        var ratio = totalHeight / (totalLines * lineHeight);

        // 分组连续的行
        var groupedIndexes = GroupConsecutiveIndexes(indexes);

        foreach (var group in groupedIndexes)
        {
            var first = group.First();
            var startY = first.Index * ratio * lineHeight;
            var height = Math.Min(group.Sum(x => x.Count) * ratio * lineHeight, group.Sum(x => x.Count) * lineHeight);

            rects.Add((0, startY, ScrollIndicatorWidth, height, first.Brush));
        }

        return rects;
    }


    private static List<(int Index, IBrush Brush, int Count)> GetDiffIndexes(IEnumerable<DiffPiece>? lines)
    {
        if (lines == null) return [];

        var diffIndexes = new List<(int Index, IBrush Brush, int Count)>();

        var diffPieces = lines.ToList();
        for (var i = 0; i < diffPieces.Count; i++)
        {
            var line = diffPieces.ElementAt(i);
            var brush = line.Type switch
            {
                ChangeType.Deleted => new SolidColorBrush(Color.Parse("#BBffAAcc")), // Red
                ChangeType.Imaginary => new SolidColorBrush(Color.Parse("#BBa4a4a4")), // Gray
                ChangeType.Inserted => new SolidColorBrush(Color.Parse("#BB50a74c")), // Green
                ChangeType.Modified => new SolidColorBrush(Color.Parse("#BBc9c9f2")), //  Blue
                _ => null
            };

            if (brush != null) diffIndexes.Add((i, brush, 1));
        }

        return diffIndexes;
    }


    private static List<List<(int Index, IBrush Brush, int Count)>> GroupConsecutiveIndexes(
        IEnumerable<(int Index, IBrush Brush, int Count)> indexes)
    {
        var groupedIndexes = new List<List<(int Index, IBrush Brush, int Count)>>();
        List<(int Index, IBrush Brush, int Count)>? currentGroup = null;

        foreach (var index in indexes.OrderBy(x => x.Index))
            if (currentGroup == null || index.Index != currentGroup.Last().Index + 1)
            {
                currentGroup = new List<(int Index, IBrush Brush, int Count)> { index };
                groupedIndexes.Add(currentGroup);
            }
            else
            {
                currentGroup.Add(index);
            }

        return groupedIndexes;
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
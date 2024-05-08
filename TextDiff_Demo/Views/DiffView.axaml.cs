using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using AvaloniaEdit;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using TextDiff_Demo.Behaviors;
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

        OlderEditor = OlderTextEditor;
        OlderEditor.Options.AllowScrollBelowDocument = true;

        NewerEditor = NewerTextEditor;
        NewerEditor.Options.AllowScrollBelowDocument = true;

        InitializeSyncScroll();

        OlderEditor.TextChanged += OnEdit;
        NewerEditor.TextChanged += OnEdit;
    }

    private TextEditor OlderEditor { get; }
    private TextEditor NewerEditor { get; }
    private DiffViewModel ViewModel { get; }

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
        var diffResult = SideBySideDiffBuilder.Diff(oldText, newText, ignoreWhitespace, ignoreCase);

        if (diffResult == null || (!diffResult.NewText.HasDifferences && !diffResult.OldText.HasDifferences))
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
            foreach (var line in diffResult.OldText.Lines.Where(line => line.Type != ChangeType.Unchanged))
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
            foreach (var line in diffResult.NewText.Lines.Where(line => line.Type != ChangeType.Unchanged))
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

            // 设置文本
            // OlderEditor.Text = oldText;
            // NewerEditor.Text = newText;
        }

    private void InitializeSyncScroll()
    {
        new SyncScrollBehavior(OlderEditor, NewerEditor);
    }
}
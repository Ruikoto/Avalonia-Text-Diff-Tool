using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using AvaloniaEdit;
using DiffPlex.DiffBuilder;
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

        AddHighlightRenderer();
        InitializeSyncScroll();
    }

    private TextEditor OlderEditor { get; }
    private TextEditor NewerEditor { get; }
    private DiffViewModel ViewModel { get; }

    private void AddHighlightRenderer()
    {
        // 设置需要高亮的行号和行内的范围
        var linesToHighlight = new[] { 2, 3, 5 };
        var lineRangesToHighlight = new Dictionary<int, List<(int Start, int Length)>>
        {
            { 2, [(3, 5), (15, 4)] }, // 第2行，从索引3到5，以及索引15到4个字符
            { 3, [(10, 7)] },
            { 5, [(8, 10)] }
        };

        // 定义背景颜色刷
        IBrush lineBrush = new SolidColorBrush(Color.Parse("#FFDDDDDD"));
        IBrush rangeBrush = new SolidColorBrush(Color.Parse("#FFFFAACC"));

        // 创建并添加渲染器
        var highlightRenderer = new HighlightBackgroundRenderer(OlderEditor, linesToHighlight, lineRangesToHighlight, lineBrush, rangeBrush);
        NewerEditor.TextArea.TextView.BackgroundRenderers.Add(highlightRenderer);
    }

    // 刷新按钮点击事件
    private void Refresh_OnClick(object? sender, RoutedEventArgs e)
    {
        Render(OlderEditor.Document.Text, NewerEditor.Document.Text);
    }

    // 渲染差异
    private void Render(string oldText, string newText, bool ignoreWhitespace = true, bool ignoreCase = false)
    {
        var diffResult = SideBySideDiffBuilder.Diff(oldText, newText, ignoreWhitespace, ignoreCase);

        if (diffResult == null || (!diffResult.NewText.HasDifferences && !diffResult.OldText.HasDifferences))
        {
            NewerEditor.TextArea.TextView.BackgroundRenderers.Clear();
            return;
        }

        // 处理差异

        // 清空原有的高亮渲染器
        NewerEditor.TextArea.TextView.BackgroundRenderers.Clear();

        // 设置新文本

    }

    private void InitializeSyncScroll()
    {
        new SyncScrollBehavior(OlderEditor, NewerEditor);
    }
}
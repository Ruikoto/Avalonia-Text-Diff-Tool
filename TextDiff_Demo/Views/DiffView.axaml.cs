using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using DiffPlex;
using DiffPlex.Chunkers;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;

namespace TextDiff_Demo.Views;

public partial class DiffView : UserControl
{
    public DiffView()
    {
        InitializeComponent();
    }

    public static void _4test()
    {
        // SideBySideDiffBuilder.Diff();
    }
}
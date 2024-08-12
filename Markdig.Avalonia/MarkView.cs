using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Markdig.Avalonia.Entity;
using Markdig.Avalonia.Helper;
using TextMateSharp.Grammars;

namespace Markdig.Avalonia;

public class MarkView : ContentControl
{
    public event EventHandler<List<HeadingRouter>>? HeadingLoaded;

    private readonly AvaloniaRenderer _renderer = new();

    // todo 增加一个文件地址的属性，markdown文本从文件读取，设置图片的根目录为文件目录

    static MarkView()
    {
        // 第一次加载markdown解析时初始化 HighlightingHelper
        TextProperty.Changed.AddClassHandler<MarkView>((view, args) => view.HandleTextChanged(view, args));
        CodeThemeProperty.Changed.AddClassHandler<MarkView>((view, args) => view.HandleThemeChanged(view, args));
    }


    #region Text

    private void HandleTextChanged(MarkView view, AvaloniaPropertyChangedEventArgs args)
    {
        if (args.NewValue == null || args.NewValue is not string)
        {
            return;
        }

        Refresh();
    }


    public static readonly StyledProperty<string> TextProperty = AvaloniaProperty.Register<MarkView, string>(
        nameof(Text));

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    #endregion

    #region Theme

    public static readonly StyledProperty<ThemeName> CodeThemeProperty = AvaloniaProperty.Register<MarkView, ThemeName>(
        "CodeTheme", defaultValue: ThemeName.DarkPlus);

    public ThemeName CodeTheme
    {
        get => GetValue(CodeThemeProperty);
        set => SetValue(CodeThemeProperty, value);
    }

    private void HandleThemeChanged(MarkView view, AvaloniaPropertyChangedEventArgs args)
    {
        if (args.NewValue == null || args.NewValue is not ThemeName)
        {
            return;
        }

        Refresh();
    }

    #endregion


    private void Refresh()
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        var markdownPipeline = new MarkdownPipelineBuilder().UseSupportedExtensions().Build();
        var document = Markdown.Parse(Text, markdownPipeline);
        var render = _renderer.Render(document, CodeTheme);
        Content = render;
        HeadingLoaded?.Invoke(this, _renderer.Routers);
        _renderer.Routers.Clear();
        stopwatch.Stop();
        Console.WriteLine("Handle Mark Text Changed: " + stopwatch.ElapsedMilliseconds + "ms");
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        HeadingLoaded = null;
        _renderer.Dispose();
        base.OnUnloaded(e);
    }
}
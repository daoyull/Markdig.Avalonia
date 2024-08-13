using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Markdig.Avalonia.Avalonia;
using Markdig.Avalonia.Avalonia.Inline;
using Markdig.Avalonia.Entity;
using Markdig.Avalonia.Helper;
using Markdig.Renderers;
using Markdig.Syntax;
using TextMateSharp.Grammars;

namespace Markdig.Avalonia;

public class AvaloniaRenderer : RendererBase, IDisposable
{
    private readonly Stack<Panel> _stack = new();
    

    private static Action<ObjectRendererCollection> _rendererCollectionAction = DefaultRenderer;

    private static void DefaultRenderer(ObjectRendererCollection objectRenderer)
    {
        objectRenderer.Add(new ListRenderer());
        objectRenderer.Add(new HeadingRenderer());
        objectRenderer.Add(new ParagraphRenderer());
        objectRenderer.Add(new FencedCodeRenderer());
        // objectRenderer.Add(new MathBlock());
        // objectRenderer.Add(new YamlFrontMatterBlock());
        objectRenderer.Add(new QuoteBlockRenderer());
        objectRenderer.Add(new ThematicBreakRenderer());
        objectRenderer.Add(new HtmlBlockRenderer());

        // Default inline renderers
        objectRenderer.Add(new AutolinkInlineRenderer());
        objectRenderer.Add(new CodeInlineRenderer());
        objectRenderer.Add(new EmphasisInlineRenderer());
        // ObjectRenderers.Add(new HtmlEntityInlineRenderer());
        objectRenderer.Add(new LinkInlineRenderer());
        objectRenderer.Add(new LiteralInlineRenderer());

        // Extension renderers
        objectRenderer.Add(new TableRenderer());
        // ObjectRenderers.Add(new TaskListRenderer());
    }

    public static void SetRendererCollectionAction(Action<ObjectRendererCollection> action)
    {
        _rendererCollectionAction = action;
    }


    public object Render(MarkdownObject markdownObject, ThemeName themeName)
    {
        ObjectRenderers.Clear();
        _rendererCollectionAction.Invoke(ObjectRenderers);
        var allMarkObj = markdownObject.Descendants().ToList();
        var fencedCodeBlock = allMarkObj.Where(it => it is FencedCodeBlock)
            .Cast<FencedCodeBlock>().ToList();
        var grammarCache = CodeThemeManager.GeTheme(themeName);
        Task.Run(() => HandleFencedCodeBlock(fencedCodeBlock, grammarCache));
        return Render(markdownObject);
    }

    public override object Render(MarkdownObject markdownObject)
    {
        Routers.Clear();
        _stack.Push(new StackPanel());
        Write(markdownObject);
        return _stack.Pop();
    }

    private void HandleFencedCodeBlock(List<FencedCodeBlock> marks, GrammarCache grammarCache)
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        foreach (var mark in marks)
        {
            ThreadPool.QueueUserWorkItem(callback =>
            {
                var codes = grammarCache.RendererCode(mark, mark.Info);
                mark.SetData("LineCodes", codes);
            });
        }

        stopwatch.Stop();
        Debug.WriteLine("HandleOneTime: " + stopwatch.ElapsedMilliseconds + "ms");
    }

    public void Push(Panel o)
    {
        _stack.Push(o);
    }

    public void Pop(bool isAddToParent = true)
    {
        var popped = _stack.Pop();
        if (!isAddToParent)
        {
            return;
        }

        _stack.Peek().Children.Add(popped);
    }

    public void WriteLeafInline(LeafBlock leafBlock)
    {
        if (leafBlock == null) throw new ArgumentNullException(nameof(leafBlock));
        if (leafBlock.Inline != null)
        {
            foreach (var inline in leafBlock.Inline)
            {
                Write(inline);
            }
        }
    }

    public void WriteRun(Run run)
    {
        var panel = _stack.Peek();
        if (!panel.Children.Any())
        {
            panel.Children.Add(new TextBlock() { TextWrapping = TextWrapping.Wrap });
        }

        InlineCollection inlines;
        var control = panel.Children.Last();
        if (control is not TextBlock textBlock)
        {
            var block = new TextBlock();
            block.TextWrapping = TextWrapping.Wrap;
            panel.Children.Add(block);
            inlines = block.Inlines!;
        }
        else
        {
            inlines = textBlock.Inlines!;
        }

        if (run.Classes.Contains("Bold"))
        {
            inlines.Add(new Run() { Text = " " });
        }

        inlines.Add(run);
        if (run.Classes.Contains("Bold"))
        {
            inlines.Add(new Run() { Text = " " });
        }
    }

    public void WriteControl(Control control)
    {
        var panel = _stack.Peek();
        panel.Children.Add(control);
    }


    public void Dispose()
    {
        Routers.Clear();
    }

    public List<HeadingRouter> Routers { get; set; } = new();

    public void WriteTitleRouter(HeadingRouter router)
    {
        Routers.Add(router);
    }
}
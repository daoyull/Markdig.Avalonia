using Markdig.Syntax.Inlines;

namespace Markdig.Avalonia.Avalonia.Inline;

public class EmphasisInlineRenderer : AvaloniaObjectRenderer<EmphasisInline>
{
    protected override void Write(AvaloniaRenderer renderer, EmphasisInline obj)
    {
        renderer.WriteChildren(obj);
    }
}
using Markdig.Renderers;
using Markdig.Syntax;

namespace Markdig.Avalonia;

public abstract class AvaloniaObjectRenderer<TObject> : MarkdownObjectRenderer<AvaloniaRenderer, TObject>
    where TObject : MarkdownObject
{
}
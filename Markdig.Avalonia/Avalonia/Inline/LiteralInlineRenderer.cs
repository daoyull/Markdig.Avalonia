using Avalonia.Controls.Documents;
using Markdig.Avalonia.Helper;
using Markdig.Syntax.Inlines;

namespace Markdig.Avalonia.Avalonia.Inline;

public class LiteralInlineRenderer : AvaloniaObjectRenderer<LiteralInline>
{
    protected override void Write(AvaloniaRenderer renderer, LiteralInline obj)
    {
        var stringSlice = obj.Content.ToString();
        var run = new Run();
        run.Text = stringSlice;
        obj.HandleRun(run);
        renderer.WriteRun(run);
    }
}
using Avalonia.Controls.Documents;
using Markdig.Syntax.Inlines;

namespace Markdig.Avalonia.Avalonia.Inline;

public class CodeInlineRenderer : AvaloniaObjectRenderer<CodeInline>
{
    protected override void Write(AvaloniaRenderer renderer, CodeInline obj)
    {
        var run = new Run();
        run.Classes.Add(nameof(CodeInline));
        run.Text = obj.Content;
        renderer.WriteRun(run);
    }
}
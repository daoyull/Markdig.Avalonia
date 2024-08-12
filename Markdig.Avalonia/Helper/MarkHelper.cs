using Avalonia.Controls.Documents;
using Markdig.Syntax.Inlines;
using Inline = Markdig.Syntax.Inlines.Inline;

namespace Markdig.Avalonia.Helper;

public static class MarkHelper
{
    public static void HandleRun(this Inline inline, Run run)
    {
        if (IsBold(inline))
        {
            run.Classes.Add("Bold");
        }

        if (IsItalics(inline))
        {
            run.Classes.Add("Italics");
        }

        if (IsDelete(inline))
        {
            run.Classes.Add("DeleteLine");
        }
    }

    private static bool IsDelete(this Inline? inline)
    {
        if (inline == null)
        {
            return false;
        }

        if (inline is EmphasisInline emphasisInline && emphasisInline.DelimiterChar == '~')
        {
            return true;
        }

        return IsDelete(inline.Parent);
    }

    private static bool IsItalics(this Inline? inline)
    {
        if (inline == null)
        {
            return false;
        }

        if (inline is EmphasisInline emphasisInline && emphasisInline.DelimiterChar == '*' &&
            emphasisInline.DelimiterCount == 1)
        {
            return true;
        }

        return IsItalics(inline.Parent);
    }

    private static bool IsBold(this Inline? inline)
    {
        if (inline == null)
        {
            return false;
        }

        if (inline is EmphasisInline emphasisInline && emphasisInline.DelimiterChar == '*' &&
            emphasisInline.DelimiterCount >= 2)
        {
            return true;
        }

        return IsBold(inline.Parent);
    }

    /// <summary>
    /// Uses all extensions supported by <c>Markdig.Wpf</c>.
    /// </summary>
    /// <param name="pipeline">The pipeline.</param>
    /// <returns>The modified pipeline</returns>
    public static MarkdownPipelineBuilder UseSupportedExtensions(this MarkdownPipelineBuilder pipeline)
    {
        if (pipeline == null) throw new ArgumentNullException(nameof(pipeline));
        return pipeline
            .UseEmphasisExtras()
            .UseGridTables()
            .UsePipeTables()
            .UseTaskLists()
            .UseAutoLinks();
    }

    internal static string SubstringAtIndexes(this string str, int startIndex, int endIndex)
    {
        return str.Substring(startIndex, endIndex - startIndex);
    }
}
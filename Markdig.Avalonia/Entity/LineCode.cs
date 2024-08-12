namespace Markdig.Avalonia.Entity;

public class LineCode
{
    public LineCode(string text)
    {
        Text = text;
    }

    public string Text { get; set; }

    public string? Foreground { get; set; } = "#ffffff";

    public string? Background { get; set; }
}
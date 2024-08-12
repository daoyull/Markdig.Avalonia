using Avalonia.Controls;

namespace Markdig.Avalonia.Entity;

public class HeadingRouter
{
    public string? Text { get; set; }

    public int Level { get; set; }

    public Control Control { get; set; } = null!;
    
}
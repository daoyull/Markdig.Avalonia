using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace Markdig.Avalonia.Controls;

public class ImageAsync : Image
{
    static ImageAsync()
    {
        UrlProperty.Changed.AddClassHandler<ImageAsync>((image, args) => image.HandleUrlChanged(image, args));
    }

    private void HandleUrlChanged(ImageAsync image, AvaloniaPropertyChangedEventArgs args)
    {
        if (args.NewValue is not string url || string.IsNullOrEmpty(url))
        {
            return;
        }

        image.Stretch = Stretch.None;

        ThreadPool.QueueUserWorkItem(LoadImage);

        async void LoadImage(object? callback)
        {
            Bitmap? bitmap = null;
            try
            {
                var uri = new Uri(url);
                if (uri.IsFile)
                {
                    var fileBytes = await File.ReadAllBytesAsync(url);
                    bitmap = new Bitmap(new MemoryStream(fileBytes));
                }
                else
                {
                    using var httpClient = new HttpClient();
                    byte[] fileBytes = await httpClient.GetByteArrayAsync(uri);
                    bitmap = new Bitmap(new MemoryStream(fileBytes));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            if (bitmap != null)
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    image.MaxHeight = bitmap.PixelSize.Height;
                    image.MaxWidth = bitmap.PixelSize.Width;
                    image.Source = bitmap;
                });
            }
        }
    }


    public static readonly StyledProperty<string?> UrlProperty = AvaloniaProperty.Register<ImageAsync, string?>(
        nameof(Url));

    public string? Url
    {
        get => GetValue(UrlProperty);
        set => SetValue(UrlProperty, value);
    }
}
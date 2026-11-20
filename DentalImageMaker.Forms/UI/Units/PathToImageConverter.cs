using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace DentalImageMaker.Forms.UI.Units;

/// <summary>파일 경로를 썸네일로. 파일을 잠그지 않도록 바로 메모리로 읽는다(받은 그림을 다시 첨부하거나 지울 수 있게).</summary>
public sealed class PathToImageConverter : IValueConverter
{
    public int DecodeWidth { get; set; } = 240;

    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string path || !File.Exists(path))
            return null;
        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            image.DecodePixelWidth = DecodeWidth;
            image.UriSource = new Uri(path);
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch
        {
            return null; // 영상 등 그림이 아닌 파일
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DentalImageMaker.Forms;

/// <summary>
/// 카메라 구도 선택지마다 보여 줄 예제 그림.
/// 기본 예제는 Gemini 기록에서 만든 그림을 앱에 넣어 두었다(Resources/CameraExamples/{key}.jpg).
/// 사용자가 바꾼 예제는 %AppData%\DentalImageMaker\camera-examples\{key}.* 에 두고, 이쪽을 먼저 쓴다.
/// </summary>
public static class CameraExampleStore
{
    public static readonly string UserFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "DentalImageMaker", "camera-examples");

    private static readonly string[] Extensions = [".png", ".jpg", ".jpeg", ".webp"];

    public static ImageSource? Load(string key, int decodeWidth = 360)
    {
        if (string.IsNullOrEmpty(key))
            return null;

        foreach (var ext in Extensions)
        {
            var path = Path.Combine(UserFolder, key + ext);
            if (File.Exists(path))
                return Decode(new Uri(path), decodeWidth);
        }

        var pack = new Uri($"pack://application:,,,/DentalImageMaker.Forms;component/Resources/CameraExamples/{key}.jpg");
        try
        {
            // 기본 예제가 없는 선택지도 있다(위생사 시점 등). 없으면 리소스 조회가 예외를 낸다.
            if (Application.GetResourceStream(pack) is null)
                return null;
        }
        catch (IOException)
        {
            return null;
        }
        return Decode(pack, decodeWidth);
    }

    /// <summary>사용자가 고른 그림을 이 선택지의 예제로 등록한다(기존 사용자 예제는 지운다).</summary>
    public static void SetUserExample(string key, string sourceFile)
    {
        Directory.CreateDirectory(UserFolder);
        foreach (var ext in Extensions)
        {
            var old = Path.Combine(UserFolder, key + ext);
            if (File.Exists(old))
                File.Delete(old);
        }
        File.Copy(sourceFile, Path.Combine(UserFolder, key + Path.GetExtension(sourceFile).ToLowerInvariant()), overwrite: true);
    }

    private static ImageSource? Decode(Uri uri, int decodeWidth)
    {
        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad; // 파일을 잠그지 않는다(예제를 바꿀 수 있게).
            image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            image.DecodePixelWidth = decodeWidth;
            image.UriSource = uri;
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch
        {
            return null;
        }
    }
}

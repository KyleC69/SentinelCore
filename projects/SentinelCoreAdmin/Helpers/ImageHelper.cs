// Solution: SentinelCore
// Project:   SentinelCoreAdmin
// File:         ImageHelper.cs
// Author: Kyle L. Crowder
// Build Num:  082808



using System.IO;
using System.Windows.Media.Imaging;

using JetBrains.Annotations;




namespace SentinelCoreAdmin.Helpers;





public static class ImageHelper
{

    public static BitmapImage ImageFromAssetsFile([CanBeNull] string fileName)
    {
        Uri imageUri = new($"pack://application:,,,/Assets/{fileName}");
        BitmapImage image = new(imageUri);
        return image;
    }








    public static BitmapImage ImageFromString([NotNull] string data)
    {
        BitmapImage image = new();
        byte[] binaryData = Convert.FromBase64String(data);
        image.BeginInit();
        image.StreamSource = new MemoryStream(binaryData);
        image.EndInit();
        return image;
    }
}
namespace Acontplus.Utilities.IO.Images;

public static class PictureHelper
{
    // some magic bytes for the most important image formats, see Wikipedia for more
    private static readonly List<byte> Jpg = new() { 0xFF, 0xD8 };
    private static readonly List<byte> Bmp = new() { 0x42, 0x4D };
    private static readonly List<byte> Gif = new() { 0x47, 0x49, 0x46 };

    private static readonly List<byte> Png = new()
    {
        0x89,
        0x50,
        0x4E,
        0x47,
        0x0D,
        0x0A,
        0x1A,
        0x0A
    };

    private static readonly List<byte> Svg_xml_small = new()
    {
        0x3C,
        0x3F,
        0x78,
        0x6D,
        0x6C
    }; // "<?xml"

    private static readonly List<byte> Svg_xml_capital = new()
    {
        0x3C,
        0x3F,
        0x58,
        0x4D,
        0x4C
    }; // "<?XML"

    private static readonly List<byte> Svg_small = new() { 0x3C, 0x73, 0x76, 0x67 }; // "<svg"
    private static readonly List<byte> Svg_capital = new() { 0x3C, 0x53, 0x56, 0x47 }; // "<SVG"
    private static readonly List<byte> Intel_tiff = new() { 0x49, 0x49, 0x2A, 0x00 };
    private static readonly List<byte> Motorola_tiff = new() { 0x4D, 0x4D, 0x00, 0x2A };

    private static readonly List<(List<byte> magic, string extension)> ImageFormats = new()
    {
        (Jpg, "jpg"),
        (Bmp, "bmp"),
        (Gif, "gif"),
        (Png, "png"),
        (Svg_small, "svg"),
        (Svg_capital, "svg"),
        (Intel_tiff, "tif"),
        (Motorola_tiff, "tif"),
        (Svg_xml_small, "svg"),
        (Svg_xml_capital, "svg")
    };

    public static string? TryGetExtension(byte[] array)
    {
        // check for simple formats first
        foreach (var imageFormat in ImageFormats)
        {
            if (array.IsImage(imageFormat.magic))
            {
                if (imageFormat.magic != Svg_xml_small && imageFormat.magic != Svg_xml_capital)
                {
                    return imageFormat.extension;
                }

                return MatchesSvgXml(array, imageFormat.magic.Count) ? imageFormat.extension : null;
            }
        }

        return null;
    }

    private static bool MatchesSvgXml(byte[] array, int startOffset)
    {
        var readCount = startOffset; // skip XML tag
        const int maxReadCount = 1024;

        do
        {
            if (array.IsImage(Svg_small, readCount) || array.IsImage(Svg_capital, readCount))
            {
                return true;
            }

            readCount++;
        } while (readCount < maxReadCount && readCount < array.Length - 1);

        return false;
    }

    private static bool IsImage(this byte[] array, List<byte> comparer, int offset = 0)
    {
        var arrayIndex = offset;
        foreach (var c in comparer)
        {
            if (arrayIndex > array.Length - 1 || array[arrayIndex] != c)
            {
                return false;
            }

            ++arrayIndex;
        }

        return true;
    }
}

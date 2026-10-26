using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using ImageMagick;

namespace LocalPhotoViewer
{
    internal sealed class DecodedImage : IDisposable
    {
        public Image Image { get; private set; }
        public Stream Stream { get; private set; }
        public bool UsedMagick { get; private set; }

        public DecodedImage(Image image, Stream stream, bool usedMagick)
        {
            Image = image;
            Stream = stream;
            UsedMagick = usedMagick;
        }

        public Image TakeImage()
        {
            Image image = Image;
            Image = null;
            return image;
        }

        public Stream TakeStream()
        {
            Stream stream = Stream;
            Stream = null;
            return stream;
        }

        public void Dispose()
        {
            if (Image != null) Image.Dispose();
            if (Stream != null) Stream.Dispose();
            Image = null;
            Stream = null;
        }
    }

    internal static class ImageDecoder
    {
        private static readonly string[] Extensions = new string[]
        {
            "bmp", "dib", "rle", "jpg", "jpeg", "jpe", "jfif", "png", "apng", "gif", "tif", "tiff", "ico", "cur", "ani",
            "webp", "avif", "heic", "heif", "jxl", "jp2", "j2k", "jpf", "jpx", "jpm", "mj2", "psd", "psb", "xcf",
            "dds", "tga", "icb", "vda", "vst", "pcx", "pcc", "sgi", "rgb", "rgba", "bw", "exr", "hdr", "pic",
            "dpx", "cin", "fits", "fit", "fts", "pbm", "pgm", "ppm", "pnm", "pam", "pfm", "qoi", "pict", "pct", "pntg",
            "miff", "dcm", "dicom", "xwd", "wmf", "emf", "svg", "svgz", "eps", "epsf", "epsi", "ps", "pdf", "ai",
            "cdr", "cgm", "odg", "wpg", "ora", "kra", "clip", "afphoto", "jxr", "hdp", "wdp", "ktx", "ktx2", "pvr",
            "dng", "nef", "nrw", "crw", "cr2", "cr3", "arw", "srf", "sr2", "raf", "orf", "rw2", "pef", "ptx", "srw",
            "3fr", "ari", "bay", "cap", "eip", "erf", "fff", "gpr", "iiq", "k25", "kdc", "mdc", "mef", "mos", "mrw",
            "obm", "qtk", "rwl", "x3f", "jng", "mng", "iff", "lbm", "ilbm", "mac", "sun", "ras", "pcd", "pdb", "pfa",
            "pfb", "fig", "otb", "icns", "fpx", "jbig", "jbg", "wbmp", "xpm", "xbm", "viff", "vif"
        };

        private static readonly string[] CommonExtensions = new string[]
        {
            "bmp", "dib", "jpg", "jpeg", "jpe", "jfif", "png", "apng", "gif", "tif", "tiff", "ico", "cur", "ani"
        };

        private static readonly string[] ModernExtensions = new string[]
        {
            "webp", "avif", "heic", "heif", "jxl", "jp2", "j2k", "jpf", "jpx", "jpm", "mj2", "qoi", "jng", "mng"
        };

        private static readonly string[] DesignExtensions = new string[]
        {
            "psd", "psb", "xcf", "ora", "kra", "clip", "afphoto", "dds", "tga", "pcx", "sgi", "exr", "hdr", "miff",
            "svg", "svgz", "eps", "epsf", "epsi", "ps", "pdf", "ai", "cdr", "cgm", "odg", "wpg", "jxr", "hdp", "wdp",
            "ktx", "ktx2", "pvr", "dcm", "dicom", "xwd"
        };

        private static readonly string[] CameraExtensions = new string[]
        {
            "dng", "nef", "nrw", "crw", "cr2", "cr3", "arw", "srf", "sr2", "raf", "orf", "rw2", "pef", "ptx", "srw",
            "3fr", "ari", "bay", "cap", "eip", "erf", "fff", "gpr", "iiq", "k25", "kdc", "mdc", "mef", "mos", "mrw",
            "obm", "qtk", "rwl", "x3f"
        };

        private static readonly HashSet<string> ExtensionSet = new HashSet<string>(Extensions, StringComparer.OrdinalIgnoreCase);

        public static string OpenFileFilter
        {
            get
            {
                return "所有支持的图片格式|" + JoinPatterns(Extensions) +
                       "|常见图片|" + JoinPatterns(CommonExtensions) +
                       "|现代网页格式|" + JoinPatterns(ModernExtensions) +
                       "|设计与专业格式|" + JoinPatterns(DesignExtensions) +
                       "|相机 RAW|" + JoinPatterns(CameraExtensions) +
                       "|所有文件|*.*";
            }
        }

        private static string JoinPatterns(string[] extensions)
        {
            string[] patterns = new string[extensions.Length];
            for (int i = 0; i < extensions.Length; i++) patterns[i] = "*." + extensions[i];
            return String.Join(";", patterns);
        }

        public static bool IsSupportedPath(string path)
        {
            if (String.IsNullOrEmpty(path)) return false;
            return ExtensionSet.Contains(Path.GetExtension(path).TrimStart('.'));
        }

        public static DecodedImage Load(string path)
        {
            Exception systemDecoderError = null;
            MemoryStream original = new MemoryStream(File.ReadAllBytes(path));
            try
            {
                Image image = System.Drawing.Image.FromStream(original, true, true);
                if (image.Width <= 0 || image.Height <= 0)
                    throw new InvalidDataException("图片尺寸无效。");
                return new DecodedImage(image, original, false);
            }
            catch (Exception ex)
            {
                systemDecoderError = ex;
                original.Dispose();
            }

            MemoryStream converted = new MemoryStream();
            try
            {
                using (MagickImage image = new MagickImage(path))
                {
                    image.AutoOrient();
                    image.Format = MagickFormat.Png;
                    image.Write(converted);
                }
                converted.Position = 0;
                Image decoded = System.Drawing.Image.FromStream(converted, true, true);
                if (decoded.Width <= 0 || decoded.Height <= 0)
                    throw new InvalidDataException("转换后的图片尺寸无效。");
                return new DecodedImage(decoded, converted, true);
            }
            catch (Exception ex)
            {
                converted.Dispose();
                throw new InvalidDataException(
                    "Windows 图片解码器和扩展解码器都无法读取此文件。文件可能已损坏，也可能需要当前安装包未包含的外部解码组件。\n\n系统解码器：" + systemDecoderError.Message,
                    ex);
            }
        }

        public static Bitmap LoadBitmap(string path)
        {
            using (DecodedImage decoded = Load(path))
                return new Bitmap(decoded.Image);
        }
    }
}

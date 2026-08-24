using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using BackgroundAutomationStudio.Native;
using BackgroundAutomationStudio.Models;
using OpenCvSharp;

namespace BackgroundAutomationStudio.Services;

public sealed record VisualMatchResult(bool Found, int CenterX, int CenterY, double Similarity, int Width, int Height)
{
    public static VisualMatchResult NotFound { get; } = new(false, 0, 0, 0, 0, 0);
}

public interface IVisualMatchingService
{
    VisualMatchResult Find(IntPtr hwnd, byte[] templatePng, int regionX, int regionY, int regionWidth, int regionHeight, double threshold);
    VisualMatchResult FindColor(IntPtr hwnd, string colorHex, int tolerance, int minimumPixels, int regionX, int regionY, int regionWidth, int regionHeight);
}

public sealed class VisualMatchingService : IVisualMatchingService
{
    private static readonly double[] Scales = [1.0, 0.9, 1.1, 0.8, 1.2];

    public VisualMatchResult Find(IntPtr hwnd, byte[] templatePng, int regionX, int regionY, int regionWidth, int regionHeight, double threshold)
    {
        if (templatePng.Length == 0) throw new InvalidOperationException("Choose a PNG template before running image scan.");
        var framePng = WindowFrameCapture.CaptureClientPng(hwnd);
        return FindInPng(framePng, templatePng, regionX, regionY, regionWidth, regionHeight, threshold);
    }

    public VisualMatchResult FindColor(IntPtr hwnd, string colorHex, int tolerance, int minimumPixels, int regionX, int regionY, int regionWidth, int regionHeight)
    {
        var framePng = WindowFrameCapture.CaptureClientPng(hwnd);
        return FindColorInPng(framePng, colorHex, tolerance, minimumPixels, regionX, regionY, regionWidth, regionHeight);
    }

    internal static VisualMatchResult FindColorInPng(byte[] framePng, string colorHex, int tolerance, int minimumPixels, int regionX, int regionY, int regionWidth, int regionHeight)
    {
        if (!ColorScanAction.TryParseColor(colorHex, out var red, out var green, out var blue)) throw new InvalidOperationException("Color must use #RRGGBB format.");
        using var frame = Cv2.ImDecode(framePng, ImreadModes.Color);
        if (frame.Empty()) throw new InvalidOperationException("The target frame could not be decoded.");
        var search = NormalizeRegion(frame.Width, frame.Height, regionX, regionY, regionWidth, regionHeight);
        using var searchColor = new Mat(frame, search);
        using var mask = new Mat();
        var safeTolerance = Math.Clamp(tolerance, 0, 255);
        var lower = new Scalar(Math.Max(0, blue - safeTolerance), Math.Max(0, green - safeTolerance), Math.Max(0, red - safeTolerance));
        var upper = new Scalar(Math.Min(255, blue + safeTolerance), Math.Min(255, green + safeTolerance), Math.Min(255, red + safeTolerance));
        Cv2.InRange(searchColor, lower, upper, mask);
        Cv2.FindContours(mask, out var contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
        Rect? bestBounds = null;
        var bestArea = 0d;
        foreach (var contour in contours)
        {
            var area = Cv2.ContourArea(contour);
            if (area < Math.Max(1, minimumPixels) || area <= bestArea) continue;
            bestArea = area; bestBounds = Cv2.BoundingRect(contour);
        }
        if (bestBounds is not { } bounds) return VisualMatchResult.NotFound;
        return new VisualMatchResult(true, search.X + bounds.X + bounds.Width / 2, search.Y + bounds.Y + bounds.Height / 2, 1, bounds.Width, bounds.Height);
    }

    internal static VisualMatchResult FindInPng(byte[] framePng, byte[] templatePng, int regionX, int regionY, int regionWidth, int regionHeight, double threshold)
    {
        using var frameColor = Cv2.ImDecode(framePng, ImreadModes.Color);
        using var templateColor = Cv2.ImDecode(templatePng, ImreadModes.Color);
        if (frameColor.Empty()) throw new InvalidOperationException("The target frame could not be decoded.");
        if (templateColor.Empty()) throw new InvalidOperationException("The selected image template is invalid.");

        var search = NormalizeRegion(frameColor.Width, frameColor.Height, regionX, regionY, regionWidth, regionHeight);
        using var searchColor = new Mat(frameColor, search);
        using var searchGray = new Mat();
        using var templateGray = new Mat();
        Cv2.CvtColor(searchColor, searchGray, ColorConversionCodes.BGR2GRAY);
        Cv2.CvtColor(templateColor, templateGray, ColorConversionCodes.BGR2GRAY);

        var best = VisualMatchResult.NotFound;
        foreach (var scale in Scales)
        {
            var width = Math.Max(2, (int)Math.Round(templateGray.Width * scale));
            var height = Math.Max(2, (int)Math.Round(templateGray.Height * scale));
            if (width > searchGray.Width || height > searchGray.Height) continue;
            using var scaled = new Mat();
            if (width == templateGray.Width && height == templateGray.Height) templateGray.CopyTo(scaled);
            else Cv2.Resize(templateGray, scaled, new OpenCvSharp.Size(width, height), 0, 0, scale < 1 ? InterpolationFlags.Area : InterpolationFlags.Linear);
            using var result = new Mat();
            Cv2.MeanStdDev(scaled, out _, out var deviation);
            var method = deviation.Val0 < 1 ? TemplateMatchModes.CCorrNormed : TemplateMatchModes.CCoeffNormed;
            Cv2.MatchTemplate(searchGray, scaled, result, method);
            Cv2.MinMaxLoc(result, out _, out var maxValue, out _, out var maxPoint);
            if (!double.IsFinite(maxValue) || maxValue <= best.Similarity) continue;
            best = new VisualMatchResult(
                maxValue >= threshold,
                search.X + maxPoint.X + width / 2,
                search.Y + maxPoint.Y + height / 2,
                maxValue,
                width,
                height);
        }
        return best.Found ? best : best with { Found = false };
    }

    private static Rect NormalizeRegion(int frameWidth, int frameHeight, int x, int y, int width, int height)
    {
        if (width <= 0 || height <= 0) return new Rect(0, 0, frameWidth, frameHeight);
        x = Math.Clamp(x, 0, Math.Max(0, frameWidth - 1));
        y = Math.Clamp(y, 0, Math.Max(0, frameHeight - 1));
        width = Math.Clamp(width, 1, frameWidth - x);
        height = Math.Clamp(height, 1, frameHeight - y);
        return new Rect(x, y, width, height);
    }
}

internal static class WindowFrameCapture
{
    public static byte[] CaptureClientPng(IntPtr hwnd)
    {
        if (!NativeMethods.GetClientRect(hwnd, out var rect) || rect.Width <= 0 || rect.Height <= 0)
            throw new InvalidOperationException("The target client area is unavailable.");

        using var bitmap = new Bitmap(rect.Width, rect.Height, PixelFormat.Format24bppRgb);
        if (IsTargetForeground(hwnd))
        {
            var origin = new POINT(0, 0);
            if (!NativeMethods.ClientToScreen(hwnd, ref origin)) throw new InvalidOperationException("The target screen position could not be read.");
            using var graphics = Graphics.FromImage(bitmap);
            graphics.CopyFromScreen(origin.X, origin.Y, 0, 0, bitmap.Size, CopyPixelOperation.SourceCopy);
        }
        else
        {
            using var graphics = Graphics.FromImage(bitmap);
            var hdc = graphics.GetHdc();
            try
            {
                if (!NativeMethods.PrintWindow(hwnd, hdc, NativeMethods.PwClientOnly))
                    throw new InvalidOperationException("The target does not support background frame capture. Bring it to the front or use a visible foreground game mode.");
            }
            finally { graphics.ReleaseHdc(hdc); }
        }

        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        return stream.ToArray();
    }

    private static bool IsTargetForeground(IntPtr hwnd)
    {
        var foreground = NativeMethods.GetForegroundWindow();
        return foreground == hwnd || NativeMethods.GetAncestor(foreground, NativeMethods.GaRoot) == hwnd;
    }
}

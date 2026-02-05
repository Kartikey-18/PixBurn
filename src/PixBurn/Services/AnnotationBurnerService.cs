using PixBurn.Models.Annotations;
using PixBurn.Services.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.Fonts;
using Color = SixLabors.ImageSharp.Color;
using PointF = SixLabors.ImageSharp.PointF;
using RectangleF = SixLabors.ImageSharp.RectangleF;

namespace PixBurn.Services;

public class AnnotationBurnerService : IAnnotationBurner
{
    private static readonly FontFamily _defaultFont;

    static AnnotationBurnerService()
    {
        // Try to get Arial, fallback to any available system font
        if (SystemFonts.TryGet("Arial", out var font))
        {
            _defaultFont = font;
        }
        else
        {
            var families = SystemFonts.Families.ToList();
            _defaultFont = families.Count > 0
                ? families[0]
                : throw new InvalidOperationException("No fonts available");
        }
    }

    public byte[] BurnAnnotations(
        byte[] sourcePixels,
        int width,
        int height,
        int samplesPerPixel,
        string photometricInterpretation,
        IReadOnlyList<AnnotationBase> annotations)
    {
        if (annotations.Count == 0)
            return sourcePixels;

        // Load pixels into ImageSharp image
        using var image = samplesPerPixel switch
        {
            1 => Image.LoadPixelData<L8>(sourcePixels, width, height).CloneAs<Rgba32>(),
            3 => Image.LoadPixelData<Rgb24>(sourcePixels, width, height).CloneAs<Rgba32>(),
            _ => throw new NotSupportedException(
                $"Unsupported format: {samplesPerPixel} samples per pixel")
        };

        // Draw each annotation
        foreach (var annotation in annotations)
        {
            DrawAnnotation(image, annotation, width, height);
        }

        // Convert back to RGB bytes
        using var rgbImage = image.CloneAs<Rgb24>();
        var result = new byte[width * height * 3];
        rgbImage.CopyPixelDataTo(result);

        return result;
    }

    private void DrawAnnotation(Image<Rgba32> image, AnnotationBase annotation,
        int width, int height)
    {
        var color = Color.FromRgba(
            annotation.StrokeColor.R,
            annotation.StrokeColor.G,
            annotation.StrokeColor.B,
            annotation.StrokeColor.A);

        switch (annotation)
        {
            case ArrowAnnotation arrow:
                DrawArrow(image, arrow, color, width, height);
                break;
            case RectangleAnnotation rect:
                DrawRectangle(image, rect, color, width, height);
                break;
            case TextAnnotation text:
                DrawText(image, text, color, width, height);
                break;
        }
    }

    private void DrawArrow(Image<Rgba32> image, ArrowAnnotation arrow,
        Color color, int width, int height)
    {
        // Convert normalized coords to pixel coords
        var start = new PointF(
            (float)(arrow.StartPoint.X * width),
            (float)(arrow.StartPoint.Y * height));
        var end = new PointF(
            (float)(arrow.EndPoint.X * width),
            (float)(arrow.EndPoint.Y * height));

        float strokeWidth = (float)arrow.StrokeWidth;

        // Draw main line
        image.Mutate(ctx => ctx.DrawLine(color, strokeWidth, start, end));

        // Draw arrowhead
        DrawArrowHead(image, start, end, color, (float)arrow.ArrowHeadSize);
    }

    private void DrawArrowHead(Image<Rgba32> image, PointF start, PointF end,
        Color color, float headSize)
    {
        // Calculate arrow direction
        float dx = end.X - start.X;
        float dy = end.Y - start.Y;
        float length = MathF.Sqrt(dx * dx + dy * dy);
        if (length < 1) return;

        float ux = dx / length;
        float uy = dy / length;

        // Perpendicular vector
        float px = -uy;
        float py = ux;

        // Arrowhead points
        float arrowBack = headSize * 0.8f;
        float arrowWidth = headSize * 0.4f;

        var p1 = new PointF(
            end.X - ux * arrowBack + px * arrowWidth,
            end.Y - uy * arrowBack + py * arrowWidth);
        var p2 = new PointF(
            end.X - ux * arrowBack - px * arrowWidth,
            end.Y - uy * arrowBack - py * arrowWidth);

        // Draw filled triangle
        var polygon = new SixLabors.ImageSharp.Drawing.Polygon(
            new LinearLineSegment(end, p1),
            new LinearLineSegment(p1, p2),
            new LinearLineSegment(p2, end));

        image.Mutate(ctx => ctx.Fill(color, polygon));
    }

    private void DrawRectangle(Image<Rgba32> image, RectangleAnnotation rect,
        Color color, int width, int height)
    {
        var bounds = new RectangleF(
            (float)(rect.Bounds.X * width),
            (float)(rect.Bounds.Y * height),
            (float)(rect.Bounds.Width * width),
            (float)(rect.Bounds.Height * height));

        if (rect.FillColor.HasValue)
        {
            var fillColor = Color.FromRgba(
                rect.FillColor.Value.R,
                rect.FillColor.Value.G,
                rect.FillColor.Value.B,
                rect.FillColor.Value.A);
            image.Mutate(ctx => ctx.Fill(fillColor, bounds));
        }

        image.Mutate(ctx => ctx.Draw(color, (float)rect.StrokeWidth, bounds));
    }

    private void DrawText(Image<Rgba32> image, TextAnnotation textAnnotation,
        Color color, int width, int height)
    {
        var position = new PointF(
            (float)(textAnnotation.Position.X * width),
            (float)(textAnnotation.Position.Y * height));

        // Scale font size based on image resolution
        float scaledFontSize = (float)textAnnotation.FontSize * (height / 500f);
        scaledFontSize = Math.Max(scaledFontSize, 12f);  // Minimum readable size

        var font = _defaultFont.CreateFont(scaledFontSize, FontStyle.Regular);

        if (textAnnotation.HasBackground)
        {
            var bgColor = Color.FromRgba(
                textAnnotation.BackgroundColor.R,
                textAnnotation.BackgroundColor.G,
                textAnnotation.BackgroundColor.B,
                textAnnotation.BackgroundColor.A);

            // Measure text and draw background
            var textOptions = new TextOptions(font);
            var bounds = TextMeasurer.MeasureBounds(textAnnotation.Text, textOptions);
            var bgRect = new RectangleF(
                position.X - 2,
                position.Y - 2,
                bounds.Width + 4,
                bounds.Height + 4);
            image.Mutate(ctx => ctx.Fill(bgColor, bgRect));
        }

        image.Mutate(ctx => ctx.DrawText(textAnnotation.Text, font, color, position));
    }
}

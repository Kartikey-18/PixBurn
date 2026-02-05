using System.IO;
using FellowOakDicom;
using FellowOakDicom.IO.Buffer;
using FellowOakDicom.Imaging;
using PixBurn.Models;
using PixBurn.Services.Interfaces;

namespace PixBurn.Services;

public class DicomWriterService : IDicomWriter
{
    public async Task<SaveResult> SaveAsync(
        string sourcePath,
        byte[] newPixelData,
        int width,
        int height,
        int samplesPerPixel,
        string outputPath,
        CancellationToken ct = default)
    {
        try
        {
            // Open original file
            var file = await DicomFile.OpenAsync(sourcePath);
            var originalDataset = file.Dataset;

            // Create new dataset with uncompressed transfer syntax
            var targetSyntax = DicomTransferSyntax.ExplicitVRLittleEndian;
            var dataset = new DicomDataset(targetSyntax);

            // Copy all non-pixel-related tags from original
            foreach (var item in originalDataset)
            {
                if (item.Tag == DicomTag.PixelData) continue;
                if (item.Tag.Group == 0x7FE0) continue; // Skip all pixel data related tags

                dataset.Add(item);
            }

            // Update image dimensions (in case they changed)
            dataset.AddOrUpdate(DicomTag.Columns, (ushort)width);
            dataset.AddOrUpdate(DicomTag.Rows, (ushort)height);
            dataset.AddOrUpdate(DicomTag.SamplesPerPixel, (ushort)samplesPerPixel);
            dataset.AddOrUpdate(DicomTag.BitsAllocated, (ushort)8);
            dataset.AddOrUpdate(DicomTag.BitsStored, (ushort)8);
            dataset.AddOrUpdate(DicomTag.HighBit, (ushort)7);
            dataset.AddOrUpdate(DicomTag.PixelRepresentation, (ushort)0);

            if (samplesPerPixel == 3)
            {
                dataset.AddOrUpdate(DicomTag.PhotometricInterpretation, "RGB");
                dataset.AddOrUpdate(DicomTag.PlanarConfiguration, (ushort)0);
            }
            else
            {
                dataset.AddOrUpdate(DicomTag.PhotometricInterpretation, "MONOCHROME2");
                dataset.Remove(DicomTag.PlanarConfiguration);
            }

            // Add pixel data
            var pixelData = DicomPixelData.Create(dataset, true);
            pixelData.AddFrame(new MemoryByteBuffer(newPixelData));

            // Update metadata to reflect modification
            dataset.AddOrUpdate(DicomTag.ContentDate, DateTime.Now.ToString("yyyyMMdd"));
            dataset.AddOrUpdate(DicomTag.ContentTime, DateTime.Now.ToString("HHmmss"));

            // Mark as derived (annotated)
            var imageType = originalDataset.GetValues<string>(DicomTag.ImageType);
            if (imageType.Length > 0)
            {
                var newImageType = new List<string>();
                if (imageType[0] != "DERIVED")
                    newImageType.Add("DERIVED");
                newImageType.AddRange(imageType.Where(t => t != "ORIGINAL" && t != "DERIVED"));
                if (newImageType.Count == 1)
                    newImageType.Add("SECONDARY");
                dataset.AddOrUpdate(DicomTag.ImageType, newImageType.ToArray());
            }
            else
            {
                dataset.AddOrUpdate(DicomTag.ImageType, "DERIVED", "SECONDARY");
            }

            // Add derivation description
            dataset.AddOrUpdate(DicomTag.DerivationDescription, "Annotations burned by PixBurn");

            // Create output directory if needed
            var outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir))
                Directory.CreateDirectory(outputDir);

            // Save with new pixel data
            var newFile = new DicomFile(dataset);
            await Task.Run(() => newFile.Save(outputPath), ct);

            return new SaveResult
            {
                SourceFile = sourcePath,
                OutputFile = outputPath,
                Success = true
            };
        }
        catch (Exception ex)
        {
            return new SaveResult
            {
                SourceFile = sourcePath,
                OutputFile = outputPath,
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}

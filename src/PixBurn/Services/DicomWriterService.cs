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

            // Use explicit VR little endian for uncompressed output
            var targetSyntax = DicomTransferSyntax.ExplicitVRLittleEndian;

            // Create new dataset with target transfer syntax
            var dataset = new DicomDataset(targetSyntax);

            // Copy all non-pixel-related tags from original
            foreach (var item in originalDataset)
            {
                // Skip pixel data and related tags
                if (item.Tag == DicomTag.PixelData) continue;
                if (item.Tag.Group == 0x7FE0) continue;

                // Skip tags that we'll be updating
                if (item.Tag == DicomTag.Rows) continue;
                if (item.Tag == DicomTag.Columns) continue;
                if (item.Tag == DicomTag.BitsAllocated) continue;
                if (item.Tag == DicomTag.BitsStored) continue;
                if (item.Tag == DicomTag.HighBit) continue;
                if (item.Tag == DicomTag.PixelRepresentation) continue;
                if (item.Tag == DicomTag.SamplesPerPixel) continue;
                if (item.Tag == DicomTag.PhotometricInterpretation) continue;
                if (item.Tag == DicomTag.PlanarConfiguration) continue;

                // Skip VOI LUT tags - they don't apply to RGB images
                if (item.Tag == DicomTag.WindowCenter) continue;
                if (item.Tag == DicomTag.WindowWidth) continue;
                if (item.Tag == DicomTag.WindowCenterWidthExplanation) continue;
                if (item.Tag == DicomTag.VOILUTFunction) continue;
                if (item.Tag == DicomTag.RescaleIntercept) continue;
                if (item.Tag == DicomTag.RescaleSlope) continue;
                if (item.Tag == DicomTag.RescaleType) continue;

                dataset.Add(item);
            }

            // Set image parameters for RGB pixel data
            dataset.AddOrUpdate(DicomTag.Columns, (ushort)width);
            dataset.AddOrUpdate(DicomTag.Rows, (ushort)height);
            dataset.AddOrUpdate(DicomTag.NumberOfFrames, 1);
            dataset.AddOrUpdate(DicomTag.SamplesPerPixel, (ushort)samplesPerPixel);
            dataset.AddOrUpdate(DicomTag.BitsAllocated, (ushort)8);
            dataset.AddOrUpdate(DicomTag.BitsStored, (ushort)8);
            dataset.AddOrUpdate(DicomTag.HighBit, (ushort)7);
            dataset.AddOrUpdate(DicomTag.PixelRepresentation, (ushort)0); // unsigned

            if (samplesPerPixel == 3)
            {
                dataset.AddOrUpdate(DicomTag.PhotometricInterpretation, "RGB");
                dataset.AddOrUpdate(DicomTag.PlanarConfiguration, (ushort)0); // interleaved RGB
            }
            else
            {
                dataset.AddOrUpdate(DicomTag.PhotometricInterpretation, "MONOCHROME2");
                dataset.Remove(DicomTag.PlanarConfiguration);
                // For grayscale, set default window/level
                dataset.AddOrUpdate(DicomTag.WindowCenter, 127.5);
                dataset.AddOrUpdate(DicomTag.WindowWidth, 255.0);
            }

            // Remove any compression-related tags
            dataset.Remove(DicomTag.LossyImageCompression);
            dataset.Remove(DicomTag.LossyImageCompressionRatio);
            dataset.Remove(DicomTag.LossyImageCompressionMethod);

            // Add the new pixel data
            var pixelData = DicomPixelData.Create(dataset, true);
            pixelData.AddFrame(new MemoryByteBuffer(newPixelData));

            // Update metadata to reflect modification
            dataset.AddOrUpdate(DicomTag.ContentDate, DateTime.Now.ToString("yyyyMMdd"));
            dataset.AddOrUpdate(DicomTag.ContentTime, DateTime.Now.ToString("HHmmss"));

            // Mark as derived (annotated)
            try
            {
                var imageType = originalDataset.GetValues<string>(DicomTag.ImageType);
                if (imageType != null && imageType.Length > 0)
                {
                    var newImageType = new List<string> { "DERIVED" };
                    foreach (var t in imageType)
                    {
                        if (t != "ORIGINAL" && t != "DERIVED")
                            newImageType.Add(t);
                    }
                    if (newImageType.Count == 1)
                        newImageType.Add("SECONDARY");
                    dataset.AddOrUpdate(DicomTag.ImageType, newImageType.ToArray());
                }
                else
                {
                    dataset.AddOrUpdate(DicomTag.ImageType, "DERIVED", "SECONDARY");
                }
            }
            catch
            {
                dataset.AddOrUpdate(DicomTag.ImageType, "DERIVED", "SECONDARY");
            }

            // Add derivation description
            dataset.AddOrUpdate(DicomTag.DerivationDescription, "Annotations burned by PixBurn");

            // Create output directory if needed
            var outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir))
                Directory.CreateDirectory(outputDir);

            // Create new file with proper file meta info
            var newFile = new DicomFile(dataset);

            // Ensure file meta info has correct transfer syntax
            newFile.FileMetaInfo.TransferSyntax = targetSyntax;
            newFile.FileMetaInfo.MediaStorageSOPClassUID = dataset.GetSingleValueOrDefault(
                DicomTag.SOPClassUID, DicomUID.SecondaryCaptureImageStorage);
            newFile.FileMetaInfo.MediaStorageSOPInstanceUID = dataset.GetSingleValueOrDefault(
                DicomTag.SOPInstanceUID, DicomUID.Generate());

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

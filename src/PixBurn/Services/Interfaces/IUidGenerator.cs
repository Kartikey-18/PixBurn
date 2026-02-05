using FellowOakDicom;

namespace PixBurn.Services.Interfaces;

public interface IUidGenerator
{
    DicomUID GenerateStudyInstanceUid();
    DicomUID GenerateSeriesInstanceUid();
    DicomUID GenerateSopInstanceUid();
}

using System.Numerics;
using FellowOakDicom;
using PixBurn.Services.Interfaces;

namespace PixBurn.Services;

public class UidGenerator : IUidGenerator
{
    public DicomUID GenerateStudyInstanceUid() => Generate();
    public DicomUID GenerateSeriesInstanceUid() => Generate();
    public DicomUID GenerateSopInstanceUid() => Generate();

    private static DicomUID Generate()
    {
        var uuid = Guid.NewGuid();
        var bytes = uuid.ToByteArray();
        var bigInt = new BigInteger(bytes, isUnsigned: true, isBigEndian: false);
        var uid = $"2.25.{bigInt}";
        return new DicomUID(uid, "Generated UID", DicomUidType.Unknown);
    }
}

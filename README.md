# PixBurn

A Windows desktop app for viewing DICOM images and burning text/label annotations directly onto the pixel data.

## What it does

Sometimes you need to add permanent labels, arrows, or annotations to medical images before exporting or archiving. PixBurn lets you:

- View DICOM files
- Draw arrows, rectangles, and text labels on the image
- Permanently burn those annotations into the pixel data
- Save the result as a new DICOM file

The annotations become part of the image itself — they'll show up in any DICOM viewer.

## Features

- Drag and drop DICOM files or use the file browser
- Draw arrows (point at things)
- Draw rectangles (highlight areas)
- Add text labels
- Choose annotation color (red, yellow, green, cyan, white, black)
- Two save modes:
  - **Save** — overwrites the original file
  - **Save as New** — saves to a `PixBurned` subfolder
- Runs completely offline — no patient data leaves your computer

## Requirements

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## How to run

```powershell
git clone <repo-url>
cd PixBurn
dotnet run --project src\PixBurn\PixBurn.csproj
```

Or build a standalone exe:

```powershell
dotnet publish src\PixBurn -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

The exe will be in `src\PixBurn\bin\Release\net8.0-windows\win-x64\publish\`

## How to use

1. Open the app
2. Drag DICOM files onto the window (or click Import Files)
3. Select a file from the list on the left
4. Choose a tool (Arrow, Rectangle, or Text)
5. Draw on the image
6. Click "Save" or "Save as New"

## Technical notes

- Uses fo-dicom for DICOM reading/writing
- Annotations are burned using ImageSharp's drawing capabilities
- Output files are marked as DERIVED/SECONDARY
- Compressed DICOMs are decompressed before annotation, saved as uncompressed

## Disclaimer

Annotated images are marked as DERIVED/SECONDARY. Adding annotations modifies the pixel data permanently.

# Acontplus.Barcode

[![NuGet](https://img.shields.io/nuget/v/Acontplus.Barcode.svg)](https://www.nuget.org/packages/Acontplus.Barcode)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

Advanced barcode generation library with ZXing.Net integration. Supports QR codes, 1D/2D barcodes, custom styling, and high-performance image generation using SkiaSharp for cross-platform applications.

## 🚀 Features

### 📱 Barcode Formats
- **QR Codes** - High-capacity 2D barcodes with error correction
- **Code 128** - High-density linear barcode for alphanumeric data
- **Code 39** - Industrial barcode standard for alphanumeric data
- **EAN-13** - European Article Number for retail products
- **EAN-8** - Compact version of EAN for small products
- **UPC-A** - Universal Product Code for retail products
- **UPC-E** - Compact UPC for small products
- **PDF417** - High-capacity 2D barcode for large data
- **Data Matrix** - Compact 2D barcode for small items

### 🎨 Customization Options
- **Custom Colors** - Foreground and background color customization via `SKColor`
- **Size Control** - Adjustable width, height, and margins
- **Error Correction** - Configurable error correction levels for QR codes
- **Format Options** - PNG, JPEG, and other image formats
- **Cross-Platform** - SkiaSharp rendering for consistent output

## 📦 Installation

### NuGet Package Manager
``bash
Install-Package Acontplus.Barcode
``

### .NET CLI
``bash
dotnet add package Acontplus.Barcode
``

### PackageReference
``xml
<PackageReference Include="Acontplus.Barcode" Version="1.1.4" />
``

## 🎯 Quick Start

### 1. Register with Dependency Injection (recommended)

``csharp
// Program.cs
builder.Services.AddBarcode();
``

Then inject `IBarcodeService` wherever needed:

``csharp
public class MyService(IBarcodeService barcodeService)
{
    public byte[] GetQrCode(string url) =>
        barcodeService.Create(new BarcodeConfig
        {
            Text = url,
            Format = ZXing.BarcodeFormat.QR_CODE,
            Width = 300,
            Height = 300
        });
}
``

### 2. Static Usage

``csharp
using Acontplus.Barcode.Utils;
using Acontplus.Barcode.Models;

var config = new BarcodeConfig
{
    Text = "https://example.com",
    Format = ZXing.BarcodeFormat.QR_CODE,
    Width = 300,
    Height = 300
};
var qrCode = BarcodeGen.Create(config);
await File.WriteAllBytesAsync("qr-code.png", qrCode);
``

### 3. Code 128 Barcode with Custom Settings

``csharp
var config = new BarcodeConfig
{
    Text = "123456789",
    Format = ZXing.BarcodeFormat.CODE_128,
    Width = 400,
    Height = 100,
    Margin = 10,
    IncludeLabel = false,
    OutputFormat = SkiaSharp.SKEncodedImageFormat.Png,
    Quality = 100
};

var barcode = BarcodeGen.Create(config);
await File.WriteAllBytesAsync("barcode.png", barcode);
``

### 4. Multiple Barcode Formats

``csharp
var formats = new[]
{
    ZXing.BarcodeFormat.QR_CODE,
    ZXing.BarcodeFormat.CODE_128,
    ZXing.BarcodeFormat.CODE_39,
    ZXing.BarcodeFormat.EAN_13,
    ZXing.BarcodeFormat.PDF_417
};

foreach (var format in formats)
{
    var config = new BarcodeConfig
    {
        Text = "Sample Data",
        Format = format,
        Width = 300,
        Height = 100
    };

    var barcode = BarcodeGen.Create(config);
    await File.WriteAllBytesAsync($"{format}.png", barcode);
}
``

## 🔧 Advanced Usage

### QR Code with Error Correction

``csharp
var config = new BarcodeConfig
{
    Text = "Important data that needs error correction",
    Format = ZXing.BarcodeFormat.QR_CODE,
    Width = 400,
    Height = 400,
    Margin = 20,
    AdditionalOptions = new ZXing.QrCode.QrCodeEncodingOptions
    {
        ErrorCorrection = ZXing.QrCode.Internal.ErrorCorrectionLevel.H
    }
};

var qrCode = BarcodeGen.Create(config);
``

### Custom Colors

``csharp
var config = new BarcodeConfig
{
    Text = "COLOR BARCODE",
    Format = ZXing.BarcodeFormat.QR_CODE,
    Width = 300,
    Height = 300,
    ForegroundColor = SkiaSharp.SKColors.DarkBlue,
    BackgroundColor = SkiaSharp.SKColors.LightYellow
};

var barcode = BarcodeGen.Create(config);
``

### Batch Generation

``csharp
var items = new[] { "Item001", "Item002", "Item003", "Item004" };

for (int i = 0; i < items.Length; i++)
{
    var config = new BarcodeConfig
    {
        Text = items[i],
        Format = ZXing.BarcodeFormat.CODE_128,
        Width = 300,
        Height = 80
    };

    var barcode = BarcodeGen.Create(config);
    await File.WriteAllBytesAsync($"item_{i + 1}.png", barcode);
}
``

### Large Data with PDF417

``csharp
var config = new BarcodeConfig
{
    Text = "Large amount of data that needs to be encoded in a compact 2D format",
    Format = ZXing.BarcodeFormat.PDF_417,
    Width = 400,
    Height = 200
};

var barcode = BarcodeGen.Create(config);
``

## 📊 Barcode Format Comparison

| Format | Type | Data Capacity | Use Case |
|--------|------|---------------|----------|
| QR Code | 2D | High | URLs, contact info, general data |
| Code 128 | 1D | Medium | Inventory, shipping, logistics |
| Code 39 | 1D | Medium | Industrial, automotive, defense |
| EAN-13 | 1D | Low | Retail products, point of sale |
| UPC-A | 1D | Low | Retail products, North America |
| PDF417 | 2D | High | Government IDs, shipping labels |
| Data Matrix | 2D | Medium | Small items, electronics |

## 🎨 Configuration Options

### BarcodeConfig Properties

``csharp
public class BarcodeConfig
{
    public string Text { get; set; }                                     // Required: text to encode
    public ZXing.BarcodeFormat Format { get; set; }                      // Default: CODE_128
    public int Width { get; set; }                                       // Default: 300
    public int Height { get; set; }                                      // Default: 100
    public bool IncludeLabel { get; set; }                               // Default: false
    public int Margin { get; set; }                                      // Default: 10
    public SkiaSharp.SKEncodedImageFormat OutputFormat { get; set; }     // Default: Png
    public int Quality { get; set; }                                     // Default: 100 (0-100)
    public SkiaSharp.SKColor? ForegroundColor { get; set; }              // Default: Black
    public SkiaSharp.SKColor? BackgroundColor { get; set; }              // Default: White
    public ZXing.Common.EncodingOptions? AdditionalOptions { get; set; } // Format-specific options
}
``

### Error Correction Levels (QR Codes)

Set via `AdditionalOptions` using `ZXing.QrCode.QrCodeEncodingOptions`:

- **L (Low)** - 7% recovery capacity
- **M (Medium)** - 15% recovery capacity
- **Q (Quartile)** - 25% recovery capacity
- **H (High)** - 30% recovery capacity

## 🔍 Best Practices

### 1. Choose the Right Format
``csharp
// For URLs and general data
var qrConfig = new BarcodeConfig { Text = "https://example.com", Format = BarcodeFormat.QR_CODE };

// For inventory systems
var code128Config = new BarcodeConfig { Text = "INV-001", Format = BarcodeFormat.CODE_128 };

// For retail products
var eanConfig = new BarcodeConfig { Text = "5901234123457", Format = BarcodeFormat.EAN_13 };
``

### 2. Optimize Size and Quality
``csharp
// High-quality QR code for printing
var highQuality = new BarcodeConfig
{
    Text = "https://example.com",
    Format = BarcodeFormat.QR_CODE,
    Width = 600,
    Height = 600,
    AdditionalOptions = new ZXing.QrCode.QrCodeEncodingOptions
    {
        ErrorCorrection = ZXing.QrCode.Internal.ErrorCorrectionLevel.H
    }
};

// Compact barcode for web display
var compact = new BarcodeConfig
{
    Text = "123456789",
    Format = BarcodeFormat.CODE_128,
    Width = 200,
    Height = 60
};
``

### 3. Parallel Batch Processing
``csharp
var barcodes = await Task.WhenAll(items.Select(item => Task.Run(() =>
    BarcodeGen.Create(new BarcodeConfig
    {
        Text = item,
        Format = BarcodeFormat.CODE_128,
        Width = 300,
        Height = 100
    }))));
``

## 📚 API Reference

### BarcodeGen (static utility)

``csharp
public static class BarcodeGen
{
    /// <summary>Generates a barcode image and returns its raw bytes.</summary>
    public static byte[] Create(BarcodeConfig config);
}
``

### IBarcodeService (DI interface)

``csharp
public interface IBarcodeService
{
    byte[] Create(BarcodeConfig config);
}
``

Register with `services.AddBarcode()` — the default implementation delegates to `BarcodeGen.Create`.

## 🔧 Dependencies

- **.NET 10+** - Modern .NET framework
- **ZXing.Net** - Barcode encoding engine
- **ZXing.Net.Bindings.SkiaSharp** - SkiaSharp renderer for ZXing
- **SkiaSharp.NativeAssets.Linux** - Native assets for Linux containers
- **System.Drawing.Common** - Cross-platform image support
- **Microsoft.Extensions.DependencyInjection.Abstractions** - DI registration helpers

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guidelines](CONTRIBUTING.md) for details.

### Development Setup
``bash
git clone https://github.com/acontplus/acontplus-dotnet-libs.git
cd acontplus-dotnet-libs
dotnet restore
dotnet build
``

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🆘 Support

- 📧 Email: proyectos@acontplus.com
- 🐛 Issues: [GitHub Issues](https://github.com/acontplus/acontplus-dotnet-libs/issues)
- 📖 Documentation: [Wiki](https://github.com/acontplus/acontplus-dotnet-libs/wiki)

## 👨‍💻 Author

**Ivan Paz** - [@iferpaz7](https://linktr.ee/iferpaz7)

## 🏢 Company

**[Acontplus](https://www.acontplus.com)** - Software solutions

---

**Built with ❤️ for the .NET community using cutting-edge .NET features**

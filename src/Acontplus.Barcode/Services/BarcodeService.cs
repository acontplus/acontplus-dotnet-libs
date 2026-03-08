using Acontplus.Barcode.Interfaces;
using Acontplus.Barcode.Utils;

namespace Acontplus.Barcode.Services;

/// <summary>
/// Default implementation of <see cref="IBarcodeService"/> backed by <see cref="BarcodeGen"/>.
/// </summary>
public sealed class BarcodeService : IBarcodeService
{
    /// <inheritdoc/>
    public byte[] Create(BarcodeConfig config) => BarcodeGen.Create(config);
}

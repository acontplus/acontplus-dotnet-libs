namespace Acontplus.Utilities.Format;

/// <summary>
/// Provides utility methods for compressing and decompressing data using Deflate and GZip algorithms, and for decompressing DataTable columns.
/// </summary>
public static class CompressionUtils
{
    /// <summary>
    /// Compresses a byte array using the Deflate algorithm.
    /// </summary>
    /// <param name="data">The byte array to compress.</param>
    /// <returns>The compressed byte array.</returns>
    public static byte[] CompressDeflate(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        using var memoryStream = new MemoryStream();
        using (var deflateStream = new DeflateStream(memoryStream, CompressionMode.Compress, true))
        {
            deflateStream.Write(data, 0, data.Length);
        }

        return memoryStream.ToArray();
    }

    /// <summary>
    /// Decompresses a byte array that was compressed using the Deflate algorithm.
    /// </summary>
    /// <param name="data">The compressed byte array.</param>
    /// <returns>The decompressed byte array.</returns>
    public static byte[] DecompressDeflate(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        using var compressStream = new MemoryStream(data);
        using var deflateStream = new DeflateStream(compressStream, CompressionMode.Decompress);
        using var decompressedStream = new MemoryStream();
        deflateStream.CopyTo(decompressedStream);
        return decompressedStream.ToArray();
    }

    /// <summary>
    /// Compresses a byte array using the GZip algorithm.
    /// </summary>
    /// <param name="data">The byte array to compress.</param>
    /// <returns>The compressed byte array.</returns>
    public static byte[] CompressGZip(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        using var memoryStream = new MemoryStream();
        using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
        {
            gzipStream.Write(data, 0, data.Length);
        }

        return memoryStream.ToArray();
    }

    /// <summary>
    /// Decompresses a byte array that was compressed using the GZip algorithm.
    /// </summary>
    /// <param name="data">The compressed byte array.</param>
    /// <returns>The decompressed byte array.</returns>
    public static byte[] DecompressGZip(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        using var compressStream = new MemoryStream(data);
        using var gzipStream = new GZipStream(compressStream, CompressionMode.Decompress);
        using var decompressedStream = new MemoryStream();
        gzipStream.CopyTo(decompressedStream);
        return decompressedStream.ToArray();
    }

    /// <summary>
    /// Decompresses a column in a <see cref="DataSet"/> table from GZip-compressed byte arrays to UTF-8 strings.
    /// </summary>
    /// <param name="dataSet">The <see cref="DataSet"/> containing the table.</param>
    /// <param name="tableName">The name of the table to process.</param>
    /// <param name="compressedColumnName">The name of the column containing compressed data.</param>
    /// <param name="decompressedColumnName">The name of the column to store decompressed strings.</param>
    public static void DecompressColumn(DataSet dataSet, string tableName, string compressedColumnName,
        string decompressedColumnName)
    {
        ArgumentNullException.ThrowIfNull(dataSet);

        if (!dataSet.Tables.Contains(tableName))
        {
            throw new ArgumentException($"Table '{tableName}' does not exist in the DataSet.");
        }

        var table = dataSet.Tables[tableName];
        DecompressColumn(table, compressedColumnName, decompressedColumnName);
    }

    /// <summary>
    /// Decompresses a column in a <see cref="DataTable"/> from GZip-compressed byte arrays to UTF-8 strings.
    /// </summary>
    /// <param name="table">The <see cref="DataTable"/> to process.</param>
    /// <param name="compressedColumnName">The name of the column containing compressed data.</param>
    /// <param name="decompressedColumnName">The name of the column to store decompressed strings.</param>
    public static void DecompressColumn(DataTable? table, string compressedColumnName, string decompressedColumnName)
    {
        ArgumentNullException.ThrowIfNull(table);

        if (!table.Columns.Contains(compressedColumnName))
        {
            throw new ArgumentException($"Column '{compressedColumnName}' does not exist in the DataTable.");
        }

        // Add a new column to store the decompressed content if it doesn't exist
        if (!table.Columns.Contains(decompressedColumnName))
        {
            table.Columns.Add(decompressedColumnName, typeof(string));
        }

        // Loop through each row in the DataTable
        foreach (DataRow row in table.Rows)
        {
            if (row[compressedColumnName] is byte[] compressedData)
            {
                var decompressedString = Encoding.UTF8.GetString(DecompressGZip(compressedData));
                row[decompressedColumnName] = decompressedString;
            }
        }
    }
}

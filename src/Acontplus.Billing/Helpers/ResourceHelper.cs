namespace Acontplus.Billing.Helpers;

/// <summary>
/// Helper utility for accessing embedded XSD schemas and billing resources.
/// </summary>
public static class ResourceHelper
{
    /// <summary>
    /// Retrieves a manifest resource stream for an embedded XSD schema file.
    /// </summary>
    /// <param name="xsdFileName">The path or name of the XSD resource file.</param>
    /// <returns>A readable <see cref="Stream"/> containing the XSD schema.</returns>
    public static Stream GetXsdStream(string xsdFileName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"{assembly.GetName().Name}.{xsdFileName.Replace("/", ".").Replace("\\", ".")}";
        return assembly.GetManifestResourceStream(resourceName) ??
               throw new FileNotFoundException($"Resource '{resourceName}' not found.");
    }
}
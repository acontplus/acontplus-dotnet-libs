using System.Xml.Schema;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Acontplus.Core.Validation;

public class ValidationError
{
    public required string Message { get; set; }
    public XmlSeverityType Severity { get; set; }
    public int LineNumber { get; set; }
    public int LinePosition { get; set; }
}

public static class XmlValidator
{
    /// <summary>
    ///     Validates the provided XmlDocument against an XSD schema file.
    /// </summary>
    /// <param name="xmlDocument">The XML document to validate.</param>
    /// <param name="xsdStream">The stream containing the XSD schema.</param>
    /// <returns>A list of ValidationError objects containing error details.</returns>
    public static List<ValidationError> Validate(XmlDocument xmlDocument, Stream xsdStream)
    {
        var validationErrors = new List<ValidationError>();

        if (xmlDocument == null)
        {
            throw new ArgumentNullException(nameof(xmlDocument));
        }

        if (xsdStream == null)
        {
            throw new ArgumentNullException(nameof(xsdStream));
        }

        try
        {
            var schemaSet = new XmlSchemaSet();

            // Handle schema compilation warnings (like missing xmldsig-core-schema.xsd)
            // These are informational and don't prevent validation of the main document structure
            schemaSet.ValidationEventHandler += (sender, e) =>
            {
                // Only report errors, ignore warnings about missing schema imports
                // xmldsig-core-schema.xsd is for digital signatures which are optional
                if (e.Severity == XmlSeverityType.Error)
                {
                    validationErrors.Add(new ValidationError
                    {
                        Message = $"Schema compilation error: {e.Message}",
                        Severity = e.Severity,
                        LineNumber = e.Exception?.LineNumber ?? 0,
                        LinePosition = e.Exception?.LinePosition ?? 0
                    });
                }
                // Warnings are ignored (missing xmldsig schema, etc.)
            };

            schemaSet.Add(null, XmlReader.Create(xsdStream));

            // Configure XmlReaderSettings
            var settings = new XmlReaderSettings
            {
                ValidationType = ValidationType.Schema,
                Schemas = schemaSet,
                ValidationFlags = XmlSchemaValidationFlags.ReportValidationWarnings |
                                 XmlSchemaValidationFlags.ProcessSchemaLocation |
                                 XmlSchemaValidationFlags.ProcessInlineSchema
            };

            settings.ValidationEventHandler += (sender, e) =>
            {
                // Filter out warnings about missing ds:Signature elements since they're optional
                // and the xmldsig schema isn't included
                if (e.Message.Contains("ds:Signature") ||
                    e.Message.Contains("xmldsig-core-schema"))
                {
                    return; // Ignore signature-related validation issues
                }

                validationErrors.Add(new ValidationError
                {
                    Message = e.Message,
                    Severity = e.Severity,
                    LineNumber = e.Exception?.LineNumber ?? 0,
                    LinePosition = e.Exception?.LinePosition ?? 0
                });
            };

            // Validate XmlDocument
            using (var stringReader = new StringReader(xmlDocument.OuterXml))
            using (var reader = XmlReader.Create(stringReader, settings))
            {
                while (reader.Read()) { } // Read and validate the entire XML
            }
        }
        catch (XmlException ex)
        {
            validationErrors.Add(new ValidationError
            {
                Message = $"XML Exception: {ex.Message}",
                Severity = XmlSeverityType.Error
            });
        }
        catch (Exception ex)
        {
            validationErrors.Add(new ValidationError
            {
                Message = $"Unexpected Exception: {ex.Message}",
                Severity = XmlSeverityType.Error
            });
        }

        return validationErrors;
    }

    /// <summary>
    ///     Exports validation errors to a JSON file.
    /// </summary>
    /// <param name="errors">List of validation errors.</param>
    /// <param name="outputFilePath">The path to save the JSON file.</param>
    public static void ExportErrorsToJson(List<ValidationError> errors, string outputFilePath)
    {
        if (errors == null || errors.Count == 0)
        {
            return;
        }

        var json = JsonSerializer.Serialize(errors, new JsonSerializerOptions { WriteIndented = true });

        File.WriteAllText(outputFilePath, json);
    }
    /// <summary>
    /// Limpia un XML para hacerlo compatible con SQL Server
    /// </summary>
    public static string CleanXmlForSqlServer(string xml)
    {
        // Si el XML está vacío o es nulo, retornarlo tal cual
        if (string.IsNullOrWhiteSpace(xml))
            return xml;
        try
        {
            // 1. Eliminar la declaración XML (<?xml version="1.0" encoding="UTF-8"?>)
            xml = Regex.Replace(xml, @"<\?xml.*?\?>", "", RegexOptions.Singleline).TrimStart();

            // 2. Eliminar caracteres BOM (Byte Order Mark) si existen
            xml = RemoveBomChars(xml);

            // 3. Escapar caracteres < y > en contenido de texto ANTES de otras limpiezas
            xml = SafeEscapeXmlContent(xml);

            // 4. Limpiar etiquetas HTML que pueden estar mezcladas con XML
            xml = CleanHtmlTags(xml);

            // 5. Corregir ampersands no escapados (&) que no sean parte de entidades XML
            xml = EscapeUnescapedAmpersands(xml);

            // 6. Normalizar saltos de línea
            xml = NormalizeLineBreaks(xml);

            // 7. Eliminar caracteres no válidos para XML
            xml = RemoveInvalidXmlChars(xml);

            return xml;
        }
        catch (Exception)
        {
            //Log.Error(ex, "Error limpiando XML");
            // En caso de error, al menos eliminar la declaración XML
            return RemoveXmlDeclaration(xml);
        }
    }

    /// <summary>
    /// Limpia etiquetas HTML que pueden estar mezcladas con XML
    /// </summary>
    private static string CleanHtmlTags(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
            return xml;

        // 1. Convertir entidades HTML comunes a sus equivalentes XML
        xml = ConvertHtmlEntitiesToXml(xml);

        // 2. Eliminar o convertir etiquetas HTML comunes que pueden causar problemas
        xml = RemoveOrConvertHtmlTags(xml);

        // 3. Limpiar atributos HTML que no son válidos en XML
        xml = CleanHtmlAttributes(xml);

        return xml;
    }

    /// <summary>
    /// Convierte entidades HTML a sus equivalentes XML válidos
    /// </summary>
    private static string ConvertHtmlEntitiesToXml(string xml)
    {
        // Mapeo de entidades HTML comunes a XML
        var htmlToXmlEntities = new Dictionary<string, string>
    {
        { "&nbsp;", "&#160;" },      // Espacio no separable
        { "&copy;", "&#169;" },      // Copyright
        { "&reg;", "&#174;" },       // Registered
        { "&trade;", "&#8482;" },    // Trademark
        { "&hellip;", "&#8230;" },   // Ellipsis
        { "&mdash;", "&#8212;" },    // Em dash
        { "&ndash;", "&#8211;" },    // En dash
        { "&lsquo;", "&#8216;" },    // Left single quote
        { "&rsquo;", "&#8217;" },    // Right single quote
        { "&ldquo;", "&#8220;" },    // Left double quote
        { "&rdquo;", "&#8221;" },    // Right double quote
        { "&euro;", "&#8364;" },     // Euro symbol
        { "&pound;", "&#163;" },     // Pound symbol
        { "&yen;", "&#165;" },       // Yen symbol
        { "&cent;", "&#162;" },      // Cent symbol
        { "&ntilde;", "ñ" },         // Ñ minúscula
        { "&Ntilde;", "Ñ" },         // Ñ mayúscula
        { "&aacute;", "á" },         // á
        { "&eacute;", "é" },         // é
        { "&iacute;", "í" },         // í
        { "&oacute;", "ó" },         // ó
        { "&uacute;", "ú" },         // ú
        { "&Aacute;", "Á" },         // Á
        { "&Eacute;", "É" },         // É
        { "&Iacute;", "Í" },         // Í
        { "&Oacute;", "Ó" },         // Ó
        { "&Uacute;", "Ú" },         // Ú
    };

        foreach (var entity in htmlToXmlEntities)
        {
            xml = xml.Replace(entity.Key, entity.Value);
        }

        return xml;
    }

    /// <summary>
    /// Elimina o convierte etiquetas HTML problemáticas
    /// </summary>
    private static string RemoveOrConvertHtmlTags(string xml)
    {
        // Etiquetas HTML que se pueden convertir a texto plano o eliminar
        var tagsToRemove = new[]
        {
        "br", "BR",           // Salto de línea
        "hr", "HR",           // Línea horizontal
        "img", "IMG",         // Imágenes
        "script", "SCRIPT",   // Scripts
        "style", "STYLE",     // Estilos
        "meta", "META",       // Metadatos
        "link", "LINK",       // Enlaces externos
    };

        // Eliminar etiquetas auto-cerradas problemáticas
        foreach (var tag in tagsToRemove)
        {
            // Etiquetas auto-cerradas: <br/>, <hr/>, <img.../>, etc.
            xml = Regex.Replace(xml, $@"<{tag}[^>]*?/>", "", RegexOptions.IgnoreCase);
            // Etiquetas simples: <br>, <hr>, etc.
            xml = Regex.Replace(xml, $@"<{tag}[^>]*?>", "", RegexOptions.IgnoreCase);
        }

        // Eliminar contenido completo de etiquetas script y style
        xml = Regex.Replace(xml, @"<script[^>]*?>.*?</script>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        xml = Regex.Replace(xml, @"<style[^>]*?>.*?</style>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        // Convertir <br> y </br> a saltos de línea si están dentro de contenido XML
        xml = Regex.Replace(xml, @"</?br[^>]*?>", "\n", RegexOptions.IgnoreCase);

        return xml;
    }

    /// <summary>
    /// Limpia atributos HTML que pueden no ser válidos en XML
    /// </summary>
    private static string CleanHtmlAttributes(string xml)
    {
        // Atributos HTML comunes que pueden causar problemas en XML
        var problematicAttributes = new[]
        {
        "onclick", "onload", "onmouseover", "onmouseout", "onfocus", "onblur",
        "class", "id", "style", "href", "src", "alt", "title"
    };

        foreach (var attr in problematicAttributes)
        {
            // Eliminar atributos problemáticos de cualquier etiqueta
            xml = Regex.Replace(xml, $@"\s+{attr}\s*=\s*[""'][^""']*[""']", "", RegexOptions.IgnoreCase);
            xml = Regex.Replace(xml, $@"\s+{attr}\s*=\s*[^>\s]+", "", RegexOptions.IgnoreCase);
        }

        return xml;
    }

    /// <summary>
    /// Escapa los ampersands y que no sean parte de entidades XML válidas
    /// </summary>
    private static string EscapeUnescapedAmpersands(string xml)
    {
        // Patrón para encontrar ampersands no escapados
        // Un ampersand es considerado no escapado si no es seguido por:
        // 1. Una entidad XML predefinida (amp;, lt;, gt;, quot;, apos;)
        // 2. Una referencia numérica (&#123; o &#xABC;)
        // 3. El inicio de una referencia de entidad que termina con ;
        return Regex.Replace(
            xml,
            @"&(?!(amp;|lt;|gt;|quot;|apos;|#[0-9]+;|#x[0-9a-fA-F]+;|\w+;))",
            "&amp;",
            RegexOptions.IgnoreCase
        );
    }

    /// <summary>
    /// Elimina caracteres BOM (Byte Order Mark) que pueden causar problemas de codificación
    /// </summary>
    private static string RemoveBomChars(string xml)
    {
        // BOM para UTF-8: EF BB BF
        if (xml.StartsWith("\xEF\xBB\xBF"))
            xml = xml.Substring(3);

        // Otros BOM comunes
        if (xml.StartsWith("\xFE\xFF") || xml.StartsWith("\xFF\xFE"))
            xml = xml.Substring(2);

        return xml;
    }

    /// <summary>
    /// Normaliza los saltos de línea para evitar problemas con diferentes sistemas operativos
    /// </summary>
    private static string NormalizeLineBreaks(string xml)
    {
        // Convertir todos los tipos de saltos de línea a \n
        return Regex.Replace(xml, @"\r\n?|\n", "\n");
    }

    /// <summary>
    /// Elimina caracteres que no son válidos en XML según la especificación
    /// </summary>
    private static string RemoveInvalidXmlChars(string xml)
    {
        // Según la especificación XML, estos caracteres no son válidos
        return Regex.Replace(xml, @"[\x00-\x08\x0B\x0C\x0E-\x1F]", "");
    }

    /// <summary>
    /// Método original para eliminar declaración XML
    /// </summary>
    private static string RemoveXmlDeclaration(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml)) return xml;
        // Elimina cualquier declaración como <?xml version="1.0" encoding="UTF-8"?>
        return Regex.Replace(xml, @"<\?xml.*?\?>", "", RegexOptions.Singleline).TrimStart();
    }

    /// <summary>
    /// Método alternativo más agresivo para casos extremos donde hay mucho HTML mezclado
    /// </summary>
    private static string AggressiveHtmlClean(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
            return xml;

        // Eliminar TODOS los tags HTML conocidos manteniendo solo el contenido
        xml = Regex.Replace(xml, @"</?(?:div|span|p|h[1-6]|ul|ol|li|table|tr|td|th|thead|tbody|tfoot|strong|b|em|i|u|small|big)[^>]*?>", "", RegexOptions.IgnoreCase);

        // Si después de limpiar queda muy poco contenido, es probable que fuera principalmente HTML
        return xml.Trim().Length < 10 ? string.Empty : xml;
    }

    /// <summary>
    /// Limpia caracteres especiales menor que y mayor que SOLO del contenido de texto entre etiquetas.
    /// NO modifica las etiquetas XML válidas.
    /// Usa un enfoque simple: reemplaza caracteres que NO son parte de etiquetas XML válidas.
    /// </summary>
    private static string SafeEscapeXmlContent(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
            return xml;

        try
        {
            // Estrategia: Buscar patrones específicos problemáticos como <TEXTO> dentro de contenido
            // Por ejemplo: "BATERIA <BORNE NORMAL> 12V" -> "BATERIA BORNE NORMAL 12V"

            // Patrón: Busca < seguido de texto que NO es una etiqueta XML válida
            // Una etiqueta XML válida empieza con letra, /, ! o ?
            // Entonces <BORNE es inválido porque después de > hay <B que no es </

            // Patrón más específico: Busca <PALABRA> donde PALABRA no tiene espacios y está en mayúsculas
            // Esto captura <BORNE NORMAL> pero no <descripcion> ni </descripcion>
            xml = Regex.Replace(xml, @"<([A-Z\s]+)>", "$1", RegexOptions.None);

            // También remover < y > sueltos que puedan quedar
            // Pero solo si NO están formando una etiqueta válida
            // Patrón: < que NO está seguido de / o letra minúscula o ! o ?
            xml = Regex.Replace(xml, @"<(?![/a-z!?])", "&lt;", RegexOptions.IgnoreCase);

            // Remover > que NO está precedido por / o letra o "
            xml = Regex.Replace(xml, @"(?<![/a-zA-Z""])>(?!<)", "&gt;", RegexOptions.None);

            return xml;
        }
        catch (Exception)
        {
            //Log.Error(ex, "Error limpiando contenido XML");
            return xml;
        }
    }
}

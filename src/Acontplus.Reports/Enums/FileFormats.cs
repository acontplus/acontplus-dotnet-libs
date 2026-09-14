using System.ComponentModel;

namespace Acontplus.Reports.Enums;

/// <summary>
/// Defines MIME content types and file extensions supported by report rendering engines.
/// </summary>
public static class FileFormats
{
    /// <summary>
    /// Supported MIME content types for exported reports.
    /// </summary>
    public enum FileContentType
    {
        /// <summary>Portable Document Format (application/pdf).</summary>
        [Description("application/pdf")]
        PDF,

        /// <summary>Legacy Microsoft Excel spreadsheet (application/vnd.ms-excel).</summary>
        [Description("application/vnd.ms-excel")]
        EXCEL,

        /// <summary>OpenXML Microsoft Excel workbook (application/vnd.openxmlformats-officedocument.spreadsheetml.sheet).</summary>
        [Description("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
        EXCELOPENXML,

        /// <summary>OpenXML Microsoft Word document (application/vnd.openxmlformats-officedocument.wordprocessingml.document).</summary>
        [Description("application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
        WORDOPENXML,

        /// <summary>HTML5 markup document (text/html).</summary>
        [Description("text/html")]
        HTML5,

        /// <summary>JPEG image format (image/jpeg).</summary>
        [Description("image/jpeg")]
        IMAGE
    }

    /// <summary>
    /// Standard file extensions corresponding to supported report formats.
    /// </summary>
    public enum FileExtension
    {
        /// <summary>.pdf file extension.</summary>
        [Description(".pdf")]
        PDF,

        /// <summary>.xls file extension.</summary>
        [Description(".xls")]
        EXCEL,

        /// <summary>.xlsx file extension.</summary>
        [Description(".xlsx")]
        EXCELOPENXML,

        /// <summary>.docx file extension.</summary>
        [Description(".docx")]
        WORDOPENXML,

        /// <summary>.html file extension.</summary>
        [Description(".html")]
        HTML5,

        /// <summary>.jpg file extension.</summary>
        [Description(".jpg")]
        IMAGE
    }
}

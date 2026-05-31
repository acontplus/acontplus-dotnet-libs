using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using Acontplus.Billing.Interfaces.Services;

namespace Acontplus.Billing.Services.Signing;

/// <summary>
/// Signs XML comprobantes with XAdES-BES format for SRI Ecuador, replicating
/// the MITyCLibXADES output: RSA-SHA1, SHA-1 digests, and three references
/// (SignedProperties | KeyInfo | comprobante).
/// </summary>
public sealed class SriSigner : ISriSigner
{
    private const string DsNs = "http://www.w3.org/2000/09/xmldsig#";
    private const string EtsiNs = "http://uri.etsi.org/01903/v1.3.2#";
    private const string C14N = "http://www.w3.org/TR/2001/REC-xml-c14n-20010315";
    private const string RsaSha1 = "http://www.w3.org/2000/09/xmldsig#rsa-sha1";
    private const string Sha1Uri = "http://www.w3.org/2000/09/xmldsig#sha1";

    /// <inheritdoc/>
    public Task<string> SignAsync(string xmlUnsigned, string pfxPassword, byte[] pfxBytes,
                                  string claveAcceso, CancellationToken ct = default)
    {
        // Validate eagerly on the calling thread before entering Task.Run so that
        // ArgumentException is thrown synchronously (consistent with TPL conventions).
        ArgumentException.ThrowIfNullOrWhiteSpace(xmlUnsigned, nameof(xmlUnsigned));
        ArgumentException.ThrowIfNullOrWhiteSpace(pfxPassword, nameof(pfxPassword));
        ArgumentNullException.ThrowIfNull(pfxBytes, nameof(pfxBytes));

        if (string.IsNullOrWhiteSpace(claveAcceso) || claveAcceso.Length != 49)
            throw new ArgumentException(
                "La clave de acceso debe tener exactamente 49 dígitos.", nameof(claveAcceso));

        // Offload CPU-bound work to the thread pool so the calling thread
        // (ASP.NET request thread, Hangfire worker, etc.) is never blocked.
        return Task.Run(() => SignCore(xmlUnsigned, pfxPassword, pfxBytes, claveAcceso), ct);
    }

    /// <summary>Performs the synchronous XAdES-BES signing work.</summary>
    private static string SignCore(string xmlUnsigned, string pfxPassword, byte[] pfxBytes,
                                   string claveAcceso)
    {
        // EphemeralKeySet: key stays in memory, never persisted to disk or Windows key store.
        // Exportable is intentionally omitted — we only sign and read the public key;
        // we never call ExportRSAPrivateKey(). Omitting it also prevents PBES2/AES-256
        // PKCS#12 files (used by Huanataca and other modern CAs) from triggering a
        // key re-wrap operation that can fail on Linux/OpenSSL.
        using var cert = X509CertificateLoader.LoadPkcs12(pfxBytes, pfxPassword,
            X509KeyStorageFlags.EphemeralKeySet);

        if (!cert.HasPrivateKey || cert.GetRSAPrivateKey() is null)
            throw new InvalidOperationException(
                "El certificado no contiene una clave privada RSA válida.");

        // Cryptographically secure numeric IDs (avoids predictable sequential values).
        string sigId = $"Signature{GetId(10_000, 99_999)}";
        string siId = $"Signature-SignedInfo{GetId(10_000, 99_999)}";
        string spId = $"{sigId}-SignedProperties{GetId(100_000, 999_999)}";
        string certId = $"Certificate{GetId(1_000_000, 9_999_999)}";
        string refId = $"Reference-ID-{GetId(100_000, 999_999)}";
        string objId = $"{sigId}-Object{GetId(100_000, 999_999)}";
        string svId = $"SignatureValue{GetId(100_000, 999_999)}";
        string spRefId = $"SignedPropertiesID{GetId(10_000, 99_999)}";

        // Load source XML and tag the root with id="comprobante".
        var xmlDoc = new XmlDocument { PreserveWhitespace = true };
        xmlDoc.LoadXml(xmlUnsigned);
        if (xmlDoc.DocumentElement is null)
            throw new InvalidOperationException("El XML proporcionado no contiene un elemento raíz.");

        xmlDoc.DocumentElement.SetAttribute("id", "comprobante");

        // Build ds:Signature in a detached document for controlled C14N serialisation.
        var sigDoc = new XmlDocument();
        var sigEl = sigDoc.CreateElement("ds", "Signature", DsNs);
        sigEl.SetAttribute("xmlns:ds", DsNs);
        sigEl.SetAttribute("xmlns:etsi", EtsiNs);
        sigEl.SetAttribute("Id", sigId);
        sigDoc.AppendChild(sigEl);

        // ds:KeyInfo — built first so its digest can be computed before SignedInfo.
        var kiEl = BuildKeyInfo(sigDoc, cert, certId);
        sigEl.AppendChild(kiEl);

        // ds:Object > etsi:QualifyingProperties > etsi:SignedProperties
        var objEl = sigDoc.CreateElement("ds", "Object", DsNs);
        objEl.SetAttribute("Id", objId);
        var qpEl = sigDoc.CreateElement("etsi", "QualifyingProperties", EtsiNs);
        qpEl.SetAttribute("Target", $"#{sigId}");
        var spEl = BuildSignedProperties(sigDoc, cert, sigId, spId, certId, refId);
        qpEl.AppendChild(spEl);
        objEl.AppendChild(qpEl);
        sigEl.AppendChild(objEl);

        // Compute the three reference digests.
        byte[] spDigest = Sha1C14NDigest(spEl);
        byte[] kiDigest = Sha1C14NDigest(kiEl);
        byte[] compDigest = Sha1C14NDigestDoc(xmlDoc);

        // ds:SignedInfo
        var siEl = sigDoc.CreateElement("ds", "SignedInfo", DsNs);
        siEl.SetAttribute("Id", siId);
        siEl.AppendChild(Elem(sigDoc, "ds", "CanonicalizationMethod", DsNs, "Algorithm", C14N));
        siEl.AppendChild(Elem(sigDoc, "ds", "SignatureMethod", DsNs, "Algorithm", RsaSha1));

        // Reference 1: etsi:SignedProperties
        siEl.AppendChild(BuildReference(sigDoc, spRefId, $"#{spId}",
            "http://uri.etsi.org/01903#SignedProperties", spDigest));

        // Reference 2: ds:KeyInfo
        siEl.AppendChild(BuildReference(sigDoc, null, $"#{certId}", null, kiDigest));

        // Reference 3: comprobante (enveloped transform)
        var r3 = BuildReference(sigDoc, refId, "#comprobante", null, compDigest);
        var transforms = sigDoc.CreateElement("ds", "Transforms", DsNs);
        transforms.AppendChild(Elem(sigDoc, "ds", "Transform", DsNs, "Algorithm",
            "http://www.w3.org/2000/09/xmldsig#enveloped-signature"));
        r3.InsertBefore(transforms, r3.FirstChild);
        siEl.AppendChild(r3);

        // SignedInfo precedes KeyInfo in the final Signature element.
        sigEl.InsertBefore(siEl, kiEl);

        // Compute SignatureValue: RSA PKCS#1 v1.5 + SHA-1 over the C14N of SignedInfo.
        byte[] siBytes = C14NBytes(siEl);
        byte[] sigBytes;
        using (var rsa = cert.GetRSAPrivateKey()!)
            sigBytes = rsa.SignData(siBytes, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);

        // ds:SignatureValue sits between SignedInfo and KeyInfo.
        var svEl = sigDoc.CreateElement("ds", "SignatureValue", DsNs);
        svEl.SetAttribute("Id", svId);
        svEl.InnerText = Convert.ToBase64String(sigBytes);
        sigEl.InsertBefore(svEl, kiEl);

        // Import the completed signature tree into the original document.
        var importedSig = (XmlElement)xmlDoc.ImportNode(sigEl, deep: true);
        xmlDoc.DocumentElement.AppendChild(importedSig);

        // Serialize as UTF-8 without BOM, with the XML declaration retained.
        using var ms = new MemoryStream();
        using var xw = XmlWriter.Create(ms, new XmlWriterSettings
        {
            Indent = false,
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            OmitXmlDeclaration = false
        });
        xmlDoc.Save(xw);
        xw.Flush();
        return Encoding.UTF8.GetString(ms.ToArray());
    }

    // ── Private builder helpers ──────────────────────────────────────────────

    private static XmlElement BuildKeyInfo(XmlDocument d, X509Certificate2 cert, string certId)
    {
        var ki = d.CreateElement("ds", "KeyInfo", DsNs);
        ki.SetAttribute("Id", certId);

        // X509Data block
        var x509data = d.CreateElement("ds", "X509Data", DsNs);
        var x509cert = d.CreateElement("ds", "X509Certificate", DsNs);
        x509cert.InnerText = Convert.ToBase64String(cert.RawData);
        x509data.AppendChild(x509cert);
        ki.AppendChild(x509data);

        // RSAKeyValue block
        var kv = d.CreateElement("ds", "KeyValue", DsNs);
        var rsaKv = d.CreateElement("ds", "RSAKeyValue", DsNs);
        using var rsaKey = cert.GetRSAPublicKey()!;
        var p = rsaKey.ExportParameters(includePrivateParameters: false);
        var mod = d.CreateElement("ds", "Modulus", DsNs);
        mod.InnerText = Convert.ToBase64String(p.Modulus!);
        var exp = d.CreateElement("ds", "Exponent", DsNs);
        exp.InnerText = Convert.ToBase64String(p.Exponent!);
        rsaKv.AppendChild(mod);
        rsaKv.AppendChild(exp);
        kv.AppendChild(rsaKv);
        ki.AppendChild(kv);
        return ki;
    }

    private static XmlElement BuildSignedProperties(XmlDocument d, X509Certificate2 cert,
        string sigId, string spId, string certId, string refId)
    {
        var sp = d.CreateElement("etsi", "SignedProperties", EtsiNs);
        sp.SetAttribute("Id", spId);
        var ssp = d.CreateElement("etsi", "SignedSignatureProperties", EtsiNs);

        // SigningTime in Ecuador timezone (UTC-5)
        var st = d.CreateElement("etsi", "SigningTime", EtsiNs);
        st.InnerText = GetEcuadorSigningTime();
        ssp.AppendChild(st);

        // SigningCertificate > Cert > CertDigest + IssuerSerial
        var sc = d.CreateElement("etsi", "SigningCertificate", EtsiNs);
        var ce = d.CreateElement("etsi", "Cert", EtsiNs);
        var cd = d.CreateElement("etsi", "CertDigest", EtsiNs);
        var dm = d.CreateElement("ds", "DigestMethod", DsNs);
        dm.SetAttribute("Algorithm", Sha1Uri);
        var dv = d.CreateElement("ds", "DigestValue", DsNs);
        using (var sha1 = SHA1.Create())
            dv.InnerText = Convert.ToBase64String(sha1.ComputeHash(cert.RawData));
        cd.AppendChild(dm);
        cd.AppendChild(dv);
        ce.AppendChild(cd);

        var iS = d.CreateElement("etsi", "IssuerSerial", EtsiNs);
        var xn = d.CreateElement("ds", "X509IssuerName", DsNs);
        xn.InnerText = cert.IssuerName.Name;
        var xs = d.CreateElement("ds", "X509SerialNumber", DsNs);
        xs.InnerText = HexSerialToDec(cert.SerialNumber);
        iS.AppendChild(xn);
        iS.AppendChild(xs);
        ce.AppendChild(iS);
        sc.AppendChild(ce);
        ssp.AppendChild(sc);
        sp.AppendChild(ssp);

        // SignedDataObjectProperties > DataObjectFormat
        var sdop = d.CreateElement("etsi", "SignedDataObjectProperties", EtsiNs);
        var dof = d.CreateElement("etsi", "DataObjectFormat", EtsiNs);
        dof.SetAttribute("ObjectReference", $"#{refId}");
        var desc = d.CreateElement("etsi", "Description", EtsiNs);
        desc.InnerText = "contenido comprobante";
        dof.AppendChild(desc);
        var mt = d.CreateElement("etsi", "MimeType", EtsiNs);
        mt.InnerText = "text/xml";
        dof.AppendChild(mt);
        sdop.AppendChild(dof);
        sp.AppendChild(sdop);
        return sp;
    }

    private static XmlElement BuildReference(XmlDocument d, string? id, string uri,
        string? type, byte[] digest)
    {
        var r = d.CreateElement("ds", "Reference", DsNs);
        if (id is not null) r.SetAttribute("Id", id);
        r.SetAttribute("URI", uri);
        if (type is not null) r.SetAttribute("Type", type);
        var dm = d.CreateElement("ds", "DigestMethod", DsNs);
        dm.SetAttribute("Algorithm", Sha1Uri);
        r.AppendChild(dm);
        var dv = d.CreateElement("ds", "DigestValue", DsNs);
        dv.InnerText = Convert.ToBase64String(digest);
        r.AppendChild(dv);
        return r;
    }

    /// <summary>Creates an element with a single attribute; convenience for algorithm elements.</summary>
    private static XmlElement Elem(XmlDocument d, string prefix, string local,
        string ns, string attr, string attrVal)
    {
        var e = d.CreateElement(prefix, local, ns);
        e.SetAttribute(attr, attrVal);
        return e;
    }

    // ── C14N / digest helpers ────────────────────────────────────────────────

    private static byte[] Sha1C14NDigest(XmlElement el)
    {
        using var sha1 = SHA1.Create();
        return sha1.ComputeHash(C14NBytes(el));
    }

    private static byte[] Sha1C14NDigestDoc(XmlDocument doc)
    {
        using var sha1 = SHA1.Create();
        return sha1.ComputeHash(C14NBytes(doc.DocumentElement!));
    }

    /// <summary>
    /// Returns the canonical (C14N) byte representation of <paramref name="el"/>.
    /// The element is wrapped in a fresh document so that inherited namespace
    /// declarations do not bleed into the canonical form.
    /// </summary>
    private static byte[] C14NBytes(XmlElement el)
    {
        var tmp = new XmlDocument { PreserveWhitespace = true };
        tmp.LoadXml(el.OuterXml);
        var t = new XmlDsigC14NTransform();
        t.LoadInput(tmp);
        using var ms = (MemoryStream)t.GetOutput(typeof(Stream));
        return ms.ToArray();
    }

    // ── Utility helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Converts a hexadecimal certificate serial number (as returned by
    /// <see cref="X509Certificate2.SerialNumber"/>) to its decimal representation,
    /// which is what the SRI and the XAdES standard expect.
    /// </summary>
    private static string HexSerialToDec(string hex) =>
        BigInteger.Parse("0" + hex, NumberStyles.HexNumber).ToString();

    private static string GetEcuadorSigningTime()
    {
        var tz =
            TryGetTz("America/Guayaquil") ??       // IANA (Linux / macOS)
            TryGetTz("SA Pacific Standard Time") ?? // Windows
            TimeZoneInfo.CreateCustomTimeZone("EC", TimeSpan.FromHours(-5), "EC", "EC");

        return TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, tz)
                           .ToString("yyyy-MM-ddTHH:mm:ss-05:00");
    }

    private static TimeZoneInfo? TryGetTz(string id)
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
        catch { return null; }
    }

    /// <summary>
    /// Returns a cryptographically random integer in [<paramref name="min"/>, <paramref name="max"/>).
    /// Intentionally uses <see cref="RandomNumberGenerator"/> rather than <see cref="Random"/>
    /// to prevent predictable ID enumeration.
    /// </summary>
    private static int GetId(int min, int max) =>
        RandomNumberGenerator.GetInt32(min, max);
}

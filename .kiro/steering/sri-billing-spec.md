---
inclusion: fileMatch
fileMatchPattern:
  [
    "src/Acontplus.Billing/**",
    "apps/src/**/*Billing*",
    "apps/src/**/*Invoice*",
    "apps/src/**/*Factura*",
  ]
---

# SRI Electronic Billing Specification — Ecuador

Technical reference for `Acontplus.Billing`. All implementation must comply with the **SRI Ficha Técnica v2.32** (updated October 2025), the official technical specification for electronic document authorization in Ecuador.

> Source: SRI Ficha Técnica — Manual de usuario, catálogo y especificaciones técnicas, Emisión de comprobantes electrónicos, Versión 2.32, October 2025.

---

## Document Types (`codDoc`)

| Code | Document                                                  |
| ---- | --------------------------------------------------------- |
| `01` | Factura (Invoice)                                         |
| `03` | Liquidación de compra de bienes y prestación de servicios |
| `04` | Nota de Crédito                                           |
| `05` | Nota de Débito                                            |
| `06` | Guía de Remisión                                          |
| `07` | Comprobante de Retención                                  |

---

## Access Key (`claveAcceso`) — 49 digits, CRITICAL

Every electronic document has a unique 49-digit numeric access key that also serves as its authorization number.

### Structure

| Position | Field                         | Format               | Length |
| -------- | ----------------------------- | -------------------- | ------ |
| 1–8      | Issue date                    | ddmmyyyy             | 8      |
| 9–10     | Document type (`codDoc`)      | Table above          | 2      |
| 11–23    | RUC (tax ID)                  | 1234567890001        | 13     |
| 24       | Environment type              | 1=Test, 2=Production | 1      |
| 25–30    | Series                        | 001001               | 6      |
| 31–39    | Sequential number             | 000000001            | 9      |
| 40–47    | Numeric code (issuer-defined) | Numeric              | 8      |
| 48       | Emission type                 | 1=Normal             | 1      |
| 49       | Check digit (Modulo 11)       | Computed             | 1      |

**All fields must be zero-padded to their exact length. An incorrect length causes immediate rejection.**

### Check Digit — Modulo 11 Algorithm

```csharp
/// <summary>
/// Computes the Modulo 11 check digit for a 48-digit SRI access key.
/// When result == 11 → digit = 0. When result == 10 → digit = 1.
/// </summary>
public static int ComputeModulo11CheckDigit(string key48Digits)
{
    // Weights cycle: 2,3,4,5,6,7 applied right-to-left
    int[] weights = { 2, 3, 4, 5, 6, 7 };
    int sum = 0;
    for (int i = 0; i < 48; i++)
    {
        int digit = key48Digits[47 - i] - '0';
        sum += digit * weights[i % 6];
    }
    int remainder = sum % 11;
    int checkDigit = 11 - remainder;
    if (checkDigit == 11) return 0;
    if (checkDigit == 10) return 1;
    return checkDigit;
}
```

### Environment Codes

| Code | Environment             |
| ---- | ----------------------- |
| `1`  | Pruebas (Test)          |
| `2`  | Producción (Production) |

### Emission Type Codes

| Code | Type                                                       |
| ---- | ---------------------------------------------------------- |
| `1`  | Emisión normal (only valid mode for offline authorization) |

---

## Identification Types (`tipoIdentificacion`)

| Code | Type                        | Notes                          |
| ---- | --------------------------- | ------------------------------ |
| `04` | RUC                         | 13 digits                      |
| `05` | Cédula                      | 10 digits                      |
| `06` | Pasaporte                   | Variable                       |
| `07` | Venta a consumidor final    | Use `9999999999999` (13 nines) |
| `08` | Identificación del exterior | Foreign tax ID                 |

**Rules:**

- Notes de Crédito, Notas de Débito, Comprobantes de Retención: must use a specific ID type (04, 05, 06, or 08). Code `07` is NOT allowed.
- Liquidaciones de compra: code `07` is NOT allowed.
- When issuing in test environment, use `PRUEBAS SERVICIO DE RENTAS INTERNAS` as the recipient `razonSocial`.

---

## Document States

| State            | Code | Meaning                                      |
| ---------------- | ---- | -------------------------------------------- |
| En procesamiento | PPR  | Received, awaiting validation                |
| Autorizado       | AUT  | Authorized — legally valid                   |
| No autorizado    | NAT  | Rejected — must be corrected and resubmitted |

**When NAT**: reuse the **same access key and sequential number**, fix the error, resubmit.

---

## Tax Codes

### IVA (`codImpuesto = 2`)

| Code | Rate                  |
| ---- | --------------------- |
| `0`  | 0%                    |
| `2`  | 12%                   |
| `3`  | 14%                   |
| `4`  | 15%                   |
| `5`  | 5%                    |
| `6`  | No objeto de impuesto |
| `7`  | Exento de IVA         |
| `8`  | IVA diferenciado      |
| `10` | 13%                   |

### ICE (`codImpuesto = 3`)

Key codes (always calculate rates from current regulation):

| Code   | Description                                     |
| ------ | ----------------------------------------------- |
| `3011` | Cigarrillos Rubios                              |
| `3021` | Cigarrillos Negros                              |
| `3023` | Productos del Tabaco excepto Cigarrillos (150%) |
| `3031` | Bebidas Alcohólicas (75%)                       |
| `3041` | Cerveza Industrial                              |
| `3073` | Vehículos ≤ $20,000 PVP (5%)                    |
| `3075` | Vehículos PVP $20,000–$30,000 (15%)             |

### Other Taxes

| Code | Tax    |
| ---- | ------ |
| `2`  | IVA    |
| `3`  | ICE    |
| `5`  | IRBPNR |

---

## SRI Web Services

### Authorization Flow (Asynchronous — REQUIRED pattern)

```
1. Send XML → RecepcionComprobantesOffline → response: RECIBIDA
2. Wait (configurable delay, recommended 2–5 seconds minimum)
3. Poll → AutorizacionComprobantesOffline with claveAcceso
4. Check state: PPR (wait more) | AUT (done) | NAT (fix and retry)
```

**NEVER poll synchronously.** The SRI spec explicitly requires asynchronous consumption.

### Endpoint URLs

#### Test Environment (pruebas)

```
Reception:     https://celcer.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline?wsdl
Authorization: https://celcer.sri.gob.ec/comprobantes-electronicos-ws/AutorizacionComprobantesOffline?wsdl
Query:         https://celcer.sri.gob.ec/comprobantes-electronicos-ws/ConsultaComprobante?wsdl
Invoice Query: https://celcer.sri.gob.ec/comprobantes-electronicos-ws/ConsultaFactura?wsdl
```

#### Production Environment (producción)

```
Reception:     https://cel.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline?wsdl
Authorization: https://cel.sri.gob.ec/comprobantes-electronicos-ws/AutorizacionComprobantesOffline?wsdl
Query:         https://cel.sri.gob.ec/comprobantes-electronicos-ws/ConsultaComprobante?wsdl
Invoice Query: https://cel.sri.gob.ec/comprobantes-electronicos-ws/ConsultaFactura?wsdl
```

**Never hardcode SSL certificate data** — SRI can change certificates without notice.

### Reception WS — `validarComprobante`

```
Input:  byte[] xml  (signed XML document)
Output: RespuestaRecepcionComprobante
  <estado>RECIBIDA</estado>   → accepted, proceed to authorization query
  <estado>DEVUELTA</estado>   → rejected at reception (schema/signature error)
```

DEVUELTA response includes `<mensajes>` with error codes and details.

### Authorization WS — `autorizacionComprobante`

```
Input:  String claveAccesoComprobante
Output: RespuestaAutorizacionComprobante
  <estado>AUTORIZADO</estado>       → legally valid
  <estado>RECHAZADO</estado>        → rejected (business rule failure)
  <estado>EN PROCESAMIENTO</estado> → still processing, poll again
```

### Batch Authorization WS — `autorizacionComprobanteLote`

```
Input:  String claveAccesoLote
Output: RespuestaLote (state per document in the batch)
```

Batch limits: **max 50 documents or 500 KB per batch**.
Individual document limit: **max 320 KB**.

### Query WS — `consultarEstadoAutorizacionComprobante`

Returns: `AUTORIZADO` | `NO AUTORIZADO` | `PENDIENTE DE ANULAR` | `ANULADO`

Returns `RECHAZADA` (id=99) when:

- Access key date is outside the SRI's allowed date range
- Document not found in SRI databases

### Query WS — Factura Comercial Negociable

Returns: `SI` (confirmed as negotiable commercial invoice) or `RECHAZADA`

---

## XML Signature — XAdES-BES (REQUIRED)

Every XML document must be digitally signed before submission.

| Parameter        | Value         |
| ---------------- | ------------- |
| Standard         | XAdES-BES     |
| Schema version   | 1.3.2         |
| Encoding         | UTF-8         |
| Signature type   | ENVELOPED     |
| Algorithm        | RSA-SHA1      |
| Key length       | 2048 bits     |
| Certificate file | PKCS12 (.p12) |

- Signature is embedded as a node within the XML document (enveloped)
- Signed regions: all comprobante nodes + `SignedProperties` container + `KeyInfo` element
- `ds:KeyInfo` must contain the signing certificate in base64

```csharp
// Libraries used by SRI for reference:
// MITyCLibXADES, MITyCLibTSA, MITyCLibAPI, MITyCLibOCSP, MITyCLibTrust
// Use BouncyCastle or System.Security.Cryptography equivalents in .NET
```

---

## Batch XML Format

```xml
<?xml version="1.0" encoding="UTF-8"?>
<lote version="1.0.0">
  <claveAcceso>4930209500125641...</claveAcceso>  <!-- 49-digit batch key -->
  <ruc>1792104394001</ruc>
  <comprobantes>
    <comprobante><![CDATA[SIGNED_XML_1]]></comprobante>
    <comprobante><![CDATA[SIGNED_XML_2]]></comprobante>
    <!-- max 50 documents, 500 KB total -->
  </comprobantes>
</lote>
```

The batch access key follows the same 49-digit structure. Series and sequential for the batch are independent from individual document sequences.

---

## Issuer Required Fields

| #   | Field                                      | Type         | Max Length | Required |
| --- | ------------------------------------------ | ------------ | ---------- | -------- |
| 1   | RUC                                        | Numeric      | 13         | ✅       |
| 2   | Razón social                               | Alphanumeric | 300        | ✅       |
| 3   | Nombre comercial                           | Alphanumeric | 300        | Optional |
| 4   | Dirección matriz                           | Alphanumeric | 300        | ✅       |
| 5   | Dirección establecimiento emisor           | Alphanumeric | 300        | Optional |
| 6   | Código establecimiento emisor              | Numeric      | 3          | ✅       |
| 7   | Código punto de emisión                    | Numeric      | 3          | ✅       |
| 8   | Contribuyente especial (resolution number) | Numeric      | 3–13       | Optional |
| 9   | Obligado a llevar contabilidad             | SI/NO        | 2          | Optional |
| 11  | Tipo de ambiente                           | Numeric      | 1          | ✅       |
| 12  | Tipo de emisión                            | Numeric      | 1          | ✅       |

---

## Consumer Final Rules

- Use `tipoIdentificacion = 07` with identification `9999999999999`
- If invoice total **exceeds $50 USD**: buyer data is **mandatory** (cannot use consumidor final)
- Up to 15 additional info fields per document (max 300 chars each)

---

## RIMPE Contributor Rules (Annex 22)

Comprobantes emitidos por contribuyentes RIMPE must include specific fields. Two categories:

- RIMPE Emprendedor
- RIMPE Negocio Popular

---

## Large Taxpayer Rules (Grandes Contribuyentes — Annex 24)

Electronic documents issued by Grandes Contribuyentes require mandatory additional fields per Annex 24.

---

## Retention Agent Rules (Annex 21)

Comprobantes emitidos por contribuyentes designados Agentes de Retención require mandatory additional fields.

---

## ISD Retention Percentages

Updated in v2.30 (March 2025). Always use current percentages from SRI resolution — do not hardcode. Retrieve from configuration or a lookup table updated from official sources.

---

## Coding Checklist for `Acontplus.Billing`

- [ ] Access key generation validates all 49 digits with Modulo 11 check digit
- [ ] Access key is reused (same key + sequential) when resubmitting a NAT document
- [ ] WS calls are asynchronous — reception call followed by polling loop
- [ ] Polling loop is configurable (delay, max retries)
- [ ] Environment (test/production) is configurable — never hardcoded
- [ ] Batch: enforces max 50 documents / 500 KB limit
- [ ] Individual document: enforces max 320 KB limit
- [ ] XAdES-BES signature is applied before submission
- [ ] SSL certificate is not hardcoded in source code
- [ ] IVA codes and ICE codes come from a lookup — not magic numbers in code
- [ ] Consumer final: blocks `tipoIdentificacion=07` when total > $50
- [ ] `Result<T>` pattern used for all WS call outcomes — no exceptions for business failures
- [ ] Test environment uses `PRUEBAS SERVICIO DE RENTAS INTERNAS` as recipient name

---

## Version History Reference

| Version | Date     | Key Change                                              |
| ------- | -------- | ------------------------------------------------------- |
| 2.32    | Oct 2025 | Transport operator invoice requirements (Annex 25)      |
| 2.31    | Mar 2025 | New WS: consulta validez + factura comercial negociable |
| 2.30    | Mar 2025 | Updated ISD retention percentages                       |
| 2.26    | Mar 2024 | Updated IVA rates table                                 |
| 2.22    | Sep 2022 | RIMPE Emprendedor / Negocio Popular                     |
| 2.10    | Dec 2017 | Comprobante de retención ATS v2.0.0                     |

// PLACEHOLDER — regenerate before shipping iOS builds.
//
// To create real receipt-validation data:
//   Window → Unity IAP → Receipt Validation Obfuscator
//   Paste the Apple Root Certificate, then click "Obfuscate Apple".
//   That overwrites this file with real Data() bytes.
//
// IAPManager detects the empty payload below and skips Apple receipt
// validation, so iOS builds compile and run — but receipts are not
// cryptographically verified until this file is regenerated.

namespace UnityEngine.Purchasing.Security
{
    public class AppleTangle
    {
        public static byte[] Data()
        {
            return new byte[0];
        }
    }
}

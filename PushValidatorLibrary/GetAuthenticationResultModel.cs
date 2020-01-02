using System;
using System.Security.Cryptography;
using System.Text;

namespace PushValidator.Library
{
    public class GetAuthenticationResultModel
    {
        public Guid TransactionId { get; set; }
        public bool Result { get; set; }
        public string CertificateFingerprint { get; set; }
        public string ActualClientIP { get; set; }
        public bool ClientIPMatch { get; set; }
        public string ServerIP { get; set; }
        public string ServerURI { get; set; }
        public string Signature { get; set; }

        public byte[] GetCombinedByteString()
        {
            return Encoding.UTF8.GetBytes(TransactionId.ToString() + Result.ToString() + CertificateFingerprint + ServerIP + ServerURI);
        }

        public byte[] CalculateSignature(string keyString)
        {
            var key = Convert.FromBase64String(keyString);
            var data = GetCombinedByteString();
            using (var hmac = new HMACSHA256(key))
            {
                return hmac.ComputeHash(data);
            }
        }
    }
}

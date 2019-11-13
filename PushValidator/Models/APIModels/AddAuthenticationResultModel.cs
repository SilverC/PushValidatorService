using System;
using System.Security.Cryptography;
using System.Text;

namespace PushValidator.Models.APIModels
{
    public class AddAuthenticationResultModel
    {
        public Guid TransactionId { get; set; }
        public bool Result { get; set; }
        public string CertificateFingerprint { get; set; }
        public string ActualClientIP { get; set; }
        public bool ClientIPMatch { get; set; }
        public string ServerIP { get; set; }
        public string ServerURI { get; set; }

        public byte[] GetCombinedByteString()
        {
            return Encoding.UTF8.GetBytes(TransactionId + Result.ToString() + CertificateFingerprint + ActualClientIP + ServerIP + ServerURI);
        }

        public bool VerifySignature(byte[] signature)
        {
            //TODO: Figure out how to use public key saved with device
            using (var algorithm = RSA.Create() )
            {
                var result = algorithm.VerifyData(GetCombinedByteString(),
                                     signature,
                                     HashAlgorithmName.SHA256,
                                     RSASignaturePadding.Pkcs1);
                return result;
            }   
        }
    }
}

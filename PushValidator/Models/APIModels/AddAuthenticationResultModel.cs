using System;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Org.BouncyCastle.Security;

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
        public string Signature { get; set; }

        public byte[] GetCombinedByteString()
        {
            var data = TransactionId.ToString().ToUpper() + Result.ToString() + CertificateFingerprint + ServerIP + ServerURI;
            Console.WriteLine(data);
            using (var hash = SHA256.Create())
            {
                PrintByteArray(hash.ComputeHash(Encoding.UTF8.GetBytes(data)));
            }
                return Encoding.UTF8.GetBytes(data);
        }

        // Display the byte array in a readable format.
        private static void PrintByteArray(byte[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                Console.Write($"{array[i]:X2}");
                if ((i % 4) == 3) Console.Write(" ");
            }
            Console.WriteLine();
        }

        public bool VerifySignature(string publicKey)
        {
            //TODO: Figure out how to use public key saved with device
            //TODO: Test publicKey length to prevent out of bounds exception
            var publicKeyBytes = Convert.FromBase64String(publicKey);       
            var signatureBytes = Convert.FromBase64String(Signature);
            var dataBytes = GetCombinedByteString();

            Console.WriteLine("Public Key:");
            PrintByteArray(publicKeyBytes);
            Console.WriteLine("-------------------------");
            Console.WriteLine("Signature:");
            PrintByteArray(signatureBytes);
            Console.WriteLine("-------------------------");
            Console.WriteLine("Data: ");
            PrintByteArray(dataBytes);
            Console.WriteLine("-------------------------");

            var signerAlgorithm = "SHA256withECDSA";
            var signer = SignerUtilities.GetSigner(signerAlgorithm);
            var pubkey = PublicKeyFactory.CreateKey(publicKeyBytes);
            signer.Init(false, pubkey);
            signer.BlockUpdate(dataBytes, 0, dataBytes.Length);
            var result = signer.VerifySignature(signatureBytes);

            return result;
        }
    }
}

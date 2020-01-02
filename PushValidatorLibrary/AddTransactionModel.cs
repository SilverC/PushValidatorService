using System;
using System.Security.Cryptography;
using System.Text;

namespace PushValidator.Library
{
    public class AddTransactionModel
    {
        public Guid ApplicationId { get; set; }
        public string Signature { get; set; }
        public string ClientIP { get; set; }
        public string GeoLocation { get; set; }
        public string UserName { get; set; }

        public byte[] GetCombinedByteString()
        {
            return Encoding.UTF8.GetBytes(ApplicationId + ClientIP + UserName);
        }

        public byte[] CalculateSignature(string keyString)
        {
            var key = Convert.FromBase64String(keyString);
            var data = GetCombinedByteString();
            using(var hmac = new HMACSHA256(key))
            {
                return hmac.ComputeHash(data);
            }
        }
    }
}

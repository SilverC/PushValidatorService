using System;
using System.Security.Cryptography;
using System.Text;

namespace PushValidator.Models
{
    public class AuthenticationResultModel
    {
        public Guid Id { get; set; }
        public Guid TransactionId { get; set; }
        public bool Result { get; set; }
        public string CertificateFingerprint { get; set; }
        public string ActualClientIP { get; set; }
        public bool ClientIPMatch { get; set; }
        public string ServerIP { get; set; }
        public string ServerURI { get; set; }
    }
}

using System;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using PushValidator.Library;

namespace PushValidator.SDK
{
    public static class Web
    {
        /// <summary>
        /// Generate login request JSON object for PushValidator service
        /// </summary>
        /// <param name="secretKey">Symmetric key for HMAC</param>
        /// <param name="applicationId">Application Id</param>
        /// <param name="clientIp">Client IP address of authenticating user</param>
        /// <param name="userName">Username of authenticating user</param>
        /// <returns></returns>
        public static JObject GenerateRequest(string secretKey,
                                              string applicationId,
                                              string clientIp,
                                              string userName)
        {
            var request = new AddTransactionModel()
            {
                ApplicationId = Guid.Parse(applicationId),
                ClientIP = clientIp,
                UserName = userName
            };
            var signature = request.CalculateSignature(secretKey);
            request.Signature = Convert.ToBase64String(signature);

            return JObject.FromObject(request);
        }

        /// <summary>
        /// Validates an authentication result returned from the PushValidator service
        /// </summary>
        /// <param name="secretKey">Secret key used to calculate the HMAC</param>
        /// <param name="result">The authentication result to be verified</param>
        /// <returns></returns>
        public static bool ValidateResponse(string secretKey,
                                            GetAuthenticationResultModel result)
        {
            var calculatedSignature = result.CalculateSignature(secretKey);
            var signatureBytes = Convert.FromBase64String(result.Signature);
            return signatureBytes.SequenceEqual(calculatedSignature);
        }

        /// <summary>
        /// Create HMAC of data using key
        /// </summary>
        /// <param name="key">Symmetric Key</param>
        /// <param name="data">Raw data</param>
        /// <returns></returns>
        private static byte[] HmacSign(byte[] key, byte[] data)
        {
            using (var hmac = new HMACSHA256(key))
            {
                return hmac.ComputeHash(data);
            }
        }

    }
}

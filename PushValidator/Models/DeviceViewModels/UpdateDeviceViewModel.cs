using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;

namespace PushValidator.Models.DeviceViewModels
{
    public class UpdateDeviceViewModel
    {
        [Required]
        [MinLength(1)]
        //TODO: May need to add custom validation
        public string DeviceId { get; set; }

        [Required]
        [MinLength(1)]
        public string DeviceToken { get; set; }

        [Required]
        [MinLength(1)]
        public string PublicKey { get; set; }

        [Required]
        public string HMAC { get; set; }

        public byte[] GetCombinedByteString()
        {
            return Encoding.UTF8.GetBytes(DeviceId + DeviceToken + PublicKey);
        }
    }
}

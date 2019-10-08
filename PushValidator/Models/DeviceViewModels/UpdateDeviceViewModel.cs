using System.ComponentModel.DataAnnotations;

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
        //TODO: Add custom validation with other data points as inputs
        public string HMAC { get; set; }
    }
}

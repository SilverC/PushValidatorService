using System.ComponentModel.DataAnnotations;

namespace PushValidator.Models.DeviceViewModels
{
    public class AddDeviceViewModel
    {
        [Required]
        //TODO: Add custom validation to ensure not empty
        public string Id { get; set; }

        [MaxLength(255, ErrorMessage = "Device Name must less than 255 characters")]
        [Display(Name = "Device Name")]
        [Required]
        public string Name { get; set; }

        [Display(Name = "Symmetric Key")]
        [Required]
        public string SymmetricKey { get; set; }

        public string RegisterURI { get; set; }

    }
}

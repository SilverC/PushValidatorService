using System;
namespace PushValidator.Models.DeviceViewModels
{
    public class ViewDeviceViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string UserId { get; set; }
        public string DeviceToken { get; set; }
        public string SymmetricKey { get; set; }
        public string PublicKey { get; set; }
        public bool Registered { get; set; }
        public string RegisterURI { get; set; }
    }
}

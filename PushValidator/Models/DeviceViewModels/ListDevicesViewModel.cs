using System.Collections.Generic;

namespace PushValidator.Models.DeviceViewModels
{
    public class ListDevicesViewModel
    {
        public IEnumerable<DeviceModel> Devices { get; set; }
    }
}

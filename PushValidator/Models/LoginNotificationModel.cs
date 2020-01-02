using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PushValidator.Models
{
    public class LoginNotificationModel
    {
        [JsonProperty("ApplicationName")]
        public string ApplicationName { get; set; }

        [JsonProperty("TransactionId")]
        public Guid TransactionId { get; set; }

        [JsonProperty("ClientIp")]
        public string ClientIP { get; set; }

        [JsonProperty("GeoLocation")]
        public string GeoLocation { get; set; }

        [JsonProperty("UserName")]
        public string UserName { get; set; }

        [JsonProperty("Timestamp")]
        public long TimeStamp { get; set; }

        public JObject ToAPNSNotification()
        {
            var customData = JObject.FromObject(this);
            var apnsStructure = JObject.Parse("{\"aps\":{ \"alert\":\"Authentication Request\" } }");
            apnsStructure.Merge(customData);
            return apnsStructure;
        }
    }
}

﻿using System;

namespace PushValidator.Models
{
    public class TransactionModel
    {
        public Guid Id { get; set; }
        public string UserId { get; set; }
        public Guid ApplicationId { get; set; }
        public string Signature { get; set; }
        public string ClientIP { get; set; }
        public string GeoLocation { get; set; }
        public string UserName { get; set; }
    }
}

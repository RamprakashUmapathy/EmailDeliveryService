using System;
using System.Collections.Generic;
using System.Text;

namespace EmailDeliveryService.Model
{
    public partial class MailStatus
    {
        public const string Prepared = "Prepared";
        public const string Sent = "Sent";
        public const string Error = "Error";
        public const string Resent = "Resent";
        public const string Failed = "Fail";
    }
}

using System;
using System.Collections.Generic;

namespace EmailDeliveryService.Model
{
    public partial class MailStatus
    {
        public long MailId { get; set; }
        public byte LineNumber { get; set; }
        public DateTime? Date { get; set; }
        public string MailStatus1 { get; set; }
        public string MailResponseId { get; set; }
        public string Exception { get; set; }
        public virtual Mail Mail { get; set; }
    }
}

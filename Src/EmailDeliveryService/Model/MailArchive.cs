using System;
using System.Collections.Generic;

namespace EmailDeliveryService.Model
{
    public partial class MailArchive
    {
        public long Id { get; set; }
        public DateTime SentDate { get; set; }
        public Guid TemplateId { get; set; }
        public string CardId { get; set; }
        public string EmailId { get; set; }
        public string EmailIdCc { get; set; }
        public string EmailIdBcc { get; set; }
        public string BodyParametersData { get; set; }
        public virtual Template Template { get; set; }
    }
}

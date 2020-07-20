using System;
using System.Collections.Generic;

namespace EmailDeliveryService.Model
{
    public partial class Mail
    {
        public Mail()
        {
            MailStatusNavigation = new HashSet<MailStatus>();
        }

        public long Id { get; set; }
        public DateTime Date { get; set; }
        public Guid TemplateId { get; set; }
        public string CardId { get; set; }
        public string EmailId { get; set; }
        public string EmailIdCc { get; set; }
        public string EmailIdBcc { get; set; }
        public string BodyParametersData { get; set; }
        public string MailStatus { get; set; }
        public virtual Template Template { get; set; }
        public virtual ICollection<MailStatus> MailStatusNavigation { get; set; }
    }
}

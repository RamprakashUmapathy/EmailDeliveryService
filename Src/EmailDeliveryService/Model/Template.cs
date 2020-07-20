using System;
using System.Collections.Generic;

namespace EmailDeliveryService.Model
{
    public partial class Template
    {
        public Template()
        {
            MailArchives = new HashSet<MailArchive>();
            Mails = new HashSet<Mail>();
        }

        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Body { get; set; }
        public string BodyParameters { get; set; }
        public string Awssesname { get; set; }
        public virtual ICollection<MailArchive> MailArchives { get; set; }
        public virtual ICollection<Mail> Mails { get; set; }
    }
}

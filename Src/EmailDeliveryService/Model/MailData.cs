using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Numerics;
using System.Text;

namespace EmailDeliveryService.Model
{
    public class MailData
    {
        public MailData()
        {
        }
        public long Id { get; set; }
        public string CardId { get; set; }
        public string EmailId { get; set; }
        public string EmailIdCC { get; set; }
        public string EmailIdBCC { get; set; }
        public string BodyParametersData { get; set; }

        public static MailData CreateInstance(string cardId, string emailId, string emailIdCC, string emailIdBCC, string bodyparametersData)
        {
            return new MailData() { BodyParametersData = bodyparametersData, CardId = cardId, EmailId = emailId, EmailIdBCC = emailIdBCC, EmailIdCC = emailIdCC };
        }
    }
}

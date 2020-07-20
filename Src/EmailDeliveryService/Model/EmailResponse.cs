using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace EmailDeliveryService.Model
{
    class EmailResponse
    {
        public long MailId { get; set; }
        public HttpStatusCode HttpStatus { get; set; }
        public Exception AmazonException { get; set; }
        public string MessageId { get; set; }

    }
}

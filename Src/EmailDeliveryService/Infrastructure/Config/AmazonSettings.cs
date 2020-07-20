using System;
using System.Collections.Generic;
using System.Text;

namespace EmailDeliveryService.Infrastructure.Config
{
    /// <summary>
    /// represents an Amazon Simple Email Service settings
    /// </summary>
    internal class EmailSettings
    {
        /// <summary>
        /// Access Key from Amazon site https://console.aws.amazon.com/
        /// </summary>
        public string AccessKey { get;  set; }
        /// <summary>
        /// Secret key from Amazon site https://console.aws.amazon.com/
        /// </summary>
        public string SecretKey { get;  set; }
        /// <summary>
        /// Source mail address (origin or from mail address)
        /// </summary>
        public string SourceMail { get;  set; }
        /// <summary>
        /// Maximum number concurrent requests made in Amazon site for optimization
        /// </summary>
        public int EmailMaxSendRate { get;  set; }
    }
}

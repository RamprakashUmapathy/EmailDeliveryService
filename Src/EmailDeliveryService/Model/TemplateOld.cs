using System;
using System.Collections.Generic;
using System.Text;

namespace EmailDeliveryService.Model
{
    /// <summary>
    /// Defines a mail template
    /// </summary>
    public class TemplateOld
    {
        /// <summary>
        /// Defines an unique identifier
        /// </summary>
        public Guid Id { get; private set; }

        /// <summary>
        /// Name of the template
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// Body of the template
        /// </summary>
        public string Body { get; private set; }
        /// <summary>
        /// Parameter template in json format
        /// </summary>
        public string BodyParameters { get; private set; }
        /// <summary>
        /// Amazon Web Service SES template name
        /// </summary>
        public string AWSTemplateName { get; private set; }
        /// <summary>
        /// is defined in Amazon Web Service SES 
        /// </summary>
        public bool IsDefinedInAWS { get; private set; }

    }
}

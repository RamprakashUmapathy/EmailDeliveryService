using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmailDeliveryService.Utilty
{
    /// <summary>
    /// Utility class to parse the input arguments
    /// </summary>
    class CommandLineArgs
    {
        private CommandLineArgs()
        {
            ErrorMessages = new List<string>();
        }
        public string[] Arguments { get; private set; }
        public bool HelpRequested { get; private set; }
        public bool IsDataLoad { get; private set; }
        public bool IsSendMail { get; private set; }
        public bool IsRetryFailedMessages { get; private set; }
        public string TemplateName { get; private set; }
        public List<string> ErrorMessages { get; set; }
        public bool IsValid()
        {
            bool invalidArgs = Arguments.Length > 2;
            if(invalidArgs) ErrorMessages.Add("Invalid number of arguments passed");
            bool result = !string.IsNullOrEmpty(TemplateName) && (IsSendMail || IsDataLoad || IsRetryFailedMessages);
            if(!result)
            {
                if (string.IsNullOrEmpty(TemplateName))
                    ErrorMessages.Add("templatename cannot be empty");
                if(!IsDataLoad || !IsSendMail || !IsRetryFailedMessages)
                {
                    ErrorMessages.Add("one of options -p or -r or -s should be selected");
                }
            }
            return !ErrorMessages.Any();
        }
        public static String GetHelpString()
        {
            var appName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;

            StringBuilder builder = new StringBuilder(appName);
            builder.AppendLine(" Params: -t:templatename -p|-r|-s [-?]");
            builder.AppendLine("Param Details:");
            builder.AppendLine("\t-? : Help message");
            builder.AppendLine("\t-t : Name of the template stored in AWS Simple Email Service cloud");
            builder.AppendLine("\t-p : Prepares the data for sending mails");
            builder.AppendLine("\t-r : Retries sending previously failed messages");
            builder.AppendLine("\t-s : Sends mail with prepared template data ");

            return builder.ToString();
        }
        public static CommandLineArgs ParseArgs(string[] args)
        {
            CommandLineArgs parser = new CommandLineArgs
            {
                Arguments = args
            };
            foreach (string param in args)
            {
                string[] tokens = param.Split(new char[] { ':' }, 2, StringSplitOptions.RemoveEmptyEntries);
                switch (tokens[0].ToLowerInvariant())
                {
                    case "-t":
                        parser.TemplateName = (tokens != null && tokens.Length == 2) ? tokens[1] : null;
                        break;
                    case "-p":
                        parser.IsDataLoad = true;
                        break;
                    case "-r":
                        parser.IsRetryFailedMessages = true;
                        break;
                    case "-s":
                        parser.IsSendMail = true;
                        break;
                    case "/?":
                        parser.HelpRequested = true;
                        //Console.WriteLine(GetHelpString());
                        break;
                    default:
                        parser.HelpRequested = true;
                        //Console.WriteLine(GetHelpString());
                        break;
                }
            }
            return parser;

        }
    }
}

using Amazon;
using Amazon.Runtime;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Dapper;
using EmailDeliveryService.Extensions;
using EmailDeliveryService.Infrastructure;
using EmailDeliveryService.Infrastructure.Config;
using EmailDeliveryService.Model;
using EmailDeliveryService.Templates;
using EmailDeliveryService.Utilty;
using EmailDeliveryService.ViewModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace EmailDeliveryService
{
    class Program
    {
        private static ILogger Logger = null;

        private static IConfiguration Configuration = null;

        private static ServiceProvider Provider = null;

        static async Task Main(string[] args)
        {
            int exitCode = 0;
            CommandLineArgs cmdArgs = null;
            try
            {
                Stopwatch watch = new Stopwatch();
                watch.Start();

                cmdArgs = CommandLineArgs.ParseArgs(args);
                if (cmdArgs.HelpRequested)
                {
                    Console.WriteLine(CommandLineArgs.GetHelpString());
                    Environment.Exit(0);
                }
                if (!cmdArgs.IsValid())
                {
                    cmdArgs.ErrorMessages.ForEach(s => Console.WriteLine(s));
                    Console.WriteLine(CommandLineArgs.GetHelpString());
                    Environment.Exit(exitCode);
                }

                Configure(args);

                Console.ForegroundColor = ConsoleColor.Green;
                Logger.LogInformation($"Starting Application with the following command line arguments {string.Join(",", args)}");

                if (cmdArgs.IsDataLoad)
                {
                    await LoadData(cmdArgs.TemplateName);
                }
                else if (cmdArgs.IsSendMail)
                {

                    int pageSize = Configuration.GetValue<int>("PageSize", 2000);
                    EmailSettings emailSettings = Configuration.GetSection("AmazonSimpleEmailService").Get<EmailSettings>();

                    using (SqlConnection connection = new SqlConnection(Configuration.GetConnectionString("newsletters")))
                    {
                        connection.Open();
                        var template = TemplateFactory.CreateInstance(cmdArgs.TemplateName);
                        int i = 1;
                        PaginationViewModel<MailData> pagedResults = null;
                        do
                        {
                            //Get data for template 
                            pagedResults = await template.GetDataAsync(connection, i, pageSize);
                            if (!pagedResults.Data.Any())
                            {
                                string err = $"No data present in database to send mails for the template {cmdArgs.TemplateName}.";
                                throw new ApplicationException(err);
                            }
                            //Send mail
                            await SendMails(connection, emailSettings, cmdArgs.TemplateName, pagedResults.Data);
                            Logger.LogInformation($"Processed page #{i} of {pagedResults.TotalPages}.");
                            i++;
                        }
                        while (i <= pagedResults.TotalPages);
                        if (connection.State == ConnectionState.Open) connection.Close();
                    }
                    //Logger.LogInformation($"End get data");
                }
                else if (cmdArgs.IsRetryFailedMessages)
                {
                    throw new NotImplementedException();
                }

                watch.Stop();
                Logger.LogInformation($"Shutting down the Application. Total Execution Time : {watch.Elapsed.TotalSeconds:F3} seconds.");
                exitCode = 0;
            }

            catch (AggregateException e)
            {
                exitCode = -1;
                Console.ForegroundColor = ConsoleColor.Red;
                foreach (Exception inner in e.InnerExceptions)
                {
                    Logger.LogError("An error occurred while executing the following: \n {0}", inner.ToString());
                }
                Logger.LogError("Shutting down the Application with the following error(s). \n {0}", e.ToString());
            }

            catch (Exception e)
            {
                exitCode = -1;
                Console.ForegroundColor = ConsoleColor.Red;
                Logger.LogError("Shutting down the Application with the following error(s). \n {0}", e.ToString());

            }
            finally
            {
                Console.ResetColor();
                LogManager.Shutdown();
                Environment.Exit(exitCode);

            }
        }


        private static async Task SendMails(SqlConnection conn, EmailSettings settings, string templateName, IEnumerable<MailData> mailData)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            Logger.LogInformation($"Start Sending mail");
            var subsets = mailData.Partition(settings.EmailMaxSendRate);

            foreach (var subset in subsets)
            {
                List<Task<EmailResponse>> tasks = new List<Task<EmailResponse>>();

                foreach (var mail in subset)
                {
                    var tsk = SendMailAsync(settings, templateName, settings.SourceMail, mail);
                    tasks.Add(tsk);
                }
                Task t = Task.WhenAll(tasks.ToArray());
                try
                {
                    await t;
                }
                catch { }
                tasks.ForEach(f =>
               {
                   long mailId = f.Result.MailId;
                   string exception = null;
                   string mailStatus = "";
                   string mailResponseId = null;

                   if (f.IsCompletedSuccessfully)
                   {
                       mailStatus = MailStatus.Sent;
                       mailResponseId = f.Result.MessageId;
                   }
                   else if (f.IsFaulted)
                   {
                       mailStatus = MailStatus.Error;
                       foreach (Exception ex in f.Exception.Flatten().InnerExceptions)
                       {
                           exception += ex.ToString() + Environment.NewLine;
                       }
                   }
                   var parameters = new { mailid = mailId, exception, mailStatus, mailResponseId };
                   conn.Execute("nl.MailsUpdate", param: parameters, commandTimeout: 0, commandType: CommandType.StoredProcedure);
               });

            }
            stopwatch.Stop();
            Logger.LogInformation($"End Sending mail in {stopwatch.Elapsed.TotalSeconds} seconds.");
        }

        public static async Task<EmailResponse> SendMailAsync(EmailSettings settings, string templateName, string sourceMail, MailData mailData)
        {
            using (var emailClient = new AmazonSimpleEmailServiceClient(
                            new BasicAWSCredentials(settings.AccessKey, settings.SecretKey),
                            RegionEndpoint.EUCentral1))
            {
                var sendRequest = new SendTemplatedEmailRequest
                {
                    Source = sourceMail,
                    Destination = new Destination { ToAddresses = new List<string> { mailData.EmailId } },
                    Template = templateName,
                    TemplateData = mailData.BodyParametersData
                };
                SendTemplatedEmailResponse result = await emailClient.SendTemplatedEmailAsync(sendRequest);
                await Task.Delay(TimeSpan.FromSeconds(1));
                return new EmailResponse() { MailId = mailData.Id, HttpStatus = result.HttpStatusCode, MessageId = result.MessageId };
            }
            //await Task.Delay(50);
            //throw new ApplicationException();
            //return new EmailResponse() { MailId = mailData.Id, HttpStatus = System.Net.HttpStatusCode.OK, MessageId = new Guid().ToString() };
        }

        private static async Task LoadData(string templateName)
        {
            Logger.LogInformation($"Start loading data");
            using (SqlConnection nlConn = new SqlConnection(Configuration.GetConnectionString("newsletters")))
            {
                nlConn.Open();
                using SqlConnection sede = new SqlConnection(Configuration.GetConnectionString("sede"));
                sede.Open();
                Dictionary<string, IDbConnection> dataSources = new Dictionary<string, IDbConnection>
                {
                    { "newsletters", nlConn },
                    { "sede", sede }
                };

                var template = TemplateFactory.CreateInstance(templateName);
                await template.LoadDataAsync(dataSources);
            }
            Logger.LogInformation($"End loading data");
        }

        private static void Configure(string[] args)
        {
            //appSettings.json
            Configuration = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
               .Build();

            var services = new ServiceCollection();
            //Logging
            services.AddLogging();
            var provider = services.BuildServiceProvider();
            var factory = provider.GetService<ILoggerFactory>();
            factory.AddNLog();
            Logger = provider.GetService<ILogger<Program>>();
            LogManager.Configuration = new NLogLoggingConfiguration(Configuration.GetSection("NLog"));

            //EFCore
            services.AddDbContext<NewsLettersContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("newsletters")));

            Provider = provider;
        }
    }
}

using EmailDeliveryService.Infrastructure;
using EmailDeliveryService.Model;
using EmailDeliveryService.Utilty;
using EmailDeliveryService.ViewModel;
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
                    await SendMail(cmdArgs.TemplateName);
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

        private static async Task SendMail(string templateName)
        {
            Logger.LogInformation($"Start get data");
            using (SqlConnection connection = new SqlConnection(Configuration.GetConnectionString("newsletters")))
            {
                connection.Open();
                var template = TemplateFactory.CreateInstance(templateName);
                int i = 1;
                PaginationViewModel<MailData> pagedResults = null;
                do
                {
                    pagedResults = await template.GetDataAsync(connection, i, 2000);
                    i++;
                }
                while (i <= pagedResults.TotalPages);
                if (connection.State == ConnectionState.Open) connection.Close();
            }
            Logger.LogInformation($"End get data");
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

            Provider = provider;
        }
    }
}

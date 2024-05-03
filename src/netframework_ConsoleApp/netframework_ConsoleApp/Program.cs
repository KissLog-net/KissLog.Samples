using KissLog;
using KissLog.CloudListeners.Auth;
using KissLog.CloudListeners.RequestLogsListener;
using KissLog.Listeners.FileListener;
using netframework_ConsoleApp.Services;
using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace netframework_ConsoleApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Logger.SetFactory(new LoggerFactory(new Logger(url: "Program/Main")));

            ConfigureKissLog();

            IKLogger logger = Logger.Factory.Get();

            logger.Trace("Trace log");
            logger.Debug("Debug log");
            logger.Info("Information log");
            logger.Warn("Warning log");
            logger.Error("Error log");
            logger.Critical("Critical log");
            logger.Error(new NullReferenceException());

            IFooService fooService = new FooService(logger);
            fooService.Foo();

            Console.WriteLine(CreateMessage());

            var loggers = Logger.Factory.GetAll();
            Logger.NotifyListeners(loggers);
        }

        static void ConfigureKissLog()
        {
            KissLogConfiguration.Listeners
                .Add(new RequestLogsApiListener(new Application(ConfigurationManager.AppSettings["LogBee.OrganizationId"], ConfigurationManager.AppSettings["LogBee.ApplicationId"]))
                {
                    ApiUrl = ConfigurationManager.AppSettings["LogBee.ApiUrl"],
                    UseAsync = false
                });

            KissLogConfiguration.Listeners
                .Add(new LocalTextFileListener(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs")));

            // optional KissLog configuration
            KissLogConfiguration.Options
                .AppendExceptionDetails((Exception ex) =>
                {
                    StringBuilder sb = new StringBuilder();

                    if (ex is NullReferenceException nullRefException)
                    {
                        sb.AppendLine("Important: check for null references");
                    }

                    return sb.ToString();
                });

            // KissLog internal logs
            KissLogConfiguration.InternalLog = (message) =>
            {
                Debug.WriteLine(message);
            };
        }

        static string CreateMessage()
        {
            string applicationId = ConfigurationManager.AppSettings["KissLog.ApplicationId"];
            string logsUrl = $"https://kisslog.net/RequestLogs/{applicationId}/kisslogsampleapp";
            string textLogs = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");

            var sb = new StringBuilder();
            sb.AppendLine("KissLog.net logs:");
            sb.AppendLine(logsUrl);
            sb.AppendLine();
            sb.AppendLine("File logs:");
            sb.AppendLine(textLogs);

            return sb.ToString();
        }
    }
}

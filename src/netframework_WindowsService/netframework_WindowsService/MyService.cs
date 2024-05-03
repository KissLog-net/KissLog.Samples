using netframework_WindowsService.Services;
using KissLog;
using KissLog.CloudListeners.Auth;
using KissLog.CloudListeners.RequestLogsListener;
using KissLog.Listeners.FileListener;
using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Text;
using System.Timers;

namespace netframework_WindowsService
{
    partial class MyService : ServiceBase
    {
        private readonly Timer _timer = new Timer();
        private readonly int _triggerInterval = 1000;

        private IKLogger Logger = new Logger();

        public MyService()
        {
            InitializeComponent();

            ConfigureKissLog();
        }

        protected override void OnStart(string[] args)
        {
            Logger = new Logger();

            Logger.Info("Starting service");

            _timer.Elapsed += new ElapsedEventHandler(Execute);
            _timer.Interval = _triggerInterval;
            _timer.Enabled = true;
        }

        protected override void OnStop()
        {
            if (_timer != null)
                _timer.Enabled = false;

            Logger.Info("Service stopped successfully");
        }

        public void Execute(object source, ElapsedEventArgs e)
        {
            KissLog.Logger.SetFactory(new LoggerFactory(new Logger(url: "MyService/Execute")));

            IKLogger logger = KissLog.Logger.Factory.Get();
            IFooService fooService = new FooService(logger);

            logger.Trace("Trace log");
            logger.Debug("Debug log");
            logger.Info("Information log");
            logger.Warn("Warning log");
            logger.Error("Error log");
            logger.Critical("Critical log");
            logger.Error(new NullReferenceException());

            fooService.Foo();

            var loggers = KissLog.Logger.Factory.GetAll();
            KissLog.Logger.NotifyListeners(loggers);
        }

        private static void ConfigureKissLog()
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
    }
}

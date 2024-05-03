using dotnet6_WorkerService;
using dotnet6_WorkerService.Services;
using KissLog;
using KissLog.AspNetCore;
using KissLog.CloudListeners.Auth;
using KissLog.CloudListeners.RequestLogsListener;
using KissLog.Formatters;
using KissLog.Listeners.FileListener;
using System.Diagnostics;
using System.Text;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.AddLogging(logging =>
        {
            logging
                .AddConfiguration(hostContext.Configuration.GetSection("Logging"))
                .AddSimpleConsole()
                .AddKissLog(options =>
                {
                    options.Formatter = (FormatterArgs args) =>
                    {
                        if (args.Exception == null)
                            return args.DefaultValue;

                        string exceptionStr = new ExceptionFormatter().Format(args.Exception, args.Logger);
                        return string.Join(Environment.NewLine, new[] { args.DefaultValue, exceptionStr });
                    };
                });

        });

        ConfigureKissLog(hostContext.Configuration);

        services.AddTransient<IFooService, FooService>();
        services.AddHostedService<Worker>();
    })
    .Build();


void ConfigureKissLog(IConfiguration configuration)
{
    KissLogConfiguration.Listeners
        .Add(new RequestLogsApiListener(new Application(configuration["LogBee.OrganizationId"], configuration["LogBee.ApplicationId"]))
        {
            ApiUrl = configuration["LogBee.ApiUrl"],
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

string CreateMessage(IConfiguration configuration)
{
    string applicationId = configuration["KissLog.ApplicationId"];
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

IConfiguration configuration = host.Services.GetRequiredService<IConfiguration>();

Console.WriteLine(CreateMessage(configuration));

await host.RunAsync();

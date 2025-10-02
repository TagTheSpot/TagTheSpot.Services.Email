using FluentEmail.MailKitSmtp;
using MailKit.Security;
using MassTransit;
using Microsoft.Extensions.Options;
using System.Runtime.InteropServices;
using TagTheSpot.Services.Email.Consumers;
using TagTheSpot.Services.Email.Emails;
using TagTheSpot.Services.Email.Options;
using TagTheSpot.Services.Shared.Application.Extensions;
using TagTheSpot.Services.Shared.Infrastructure.Options;
using TagTheSpot.Services.Shared.Messaging.Auth;
using TagTheSpot.Services.Shared.Messaging.Submissions;

namespace TagTheSpot.Services.Email
{
    public class Program
    {
        public static void Main(string[] args)
        {
            bool runAsService = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
                                Environment.GetEnvironmentVariable("RUN_AS_SERVICE") == "true";

            var hostBuilder = Host.CreateDefaultBuilder(args)
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();

                    try
                    {
                        logging.AddFile("C:/Logs/email-service-{Date}.txt");
                    }
                    catch (Exception)
                    { }
                })
                .ConfigureServices((context, services) =>
                {
                    services.ConfigureValidatableOnStartOptions<SmtpSettings>();
                    services.ConfigureValidatableOnStartOptions<RabbitMqSettings>();
                    services.ConfigureValidatableOnStartOptions<MessagingSettings>();

                    services.AddMassTransit(cfg =>
                    {
                        cfg.AddConsumer<SendConfirmationEmailRequestedEventConsumer>();
                        cfg.AddConsumer<SendResetPasswordEmailRequestedEventConsumer>();
                        cfg.AddConsumer<SendSubmissionApprovedEmailRequestedEventConsumer>();
                        cfg.AddConsumer<SendSubmissionRejectedEmailRequestedEventConsumer>();

                        cfg.UsingRabbitMq((context, config) =>
                        {
                            var rabbitMqSettings = context.GetRequiredService<IOptions<RabbitMqSettings>>().Value;
                            var messagingSettings = context.GetRequiredService<IOptions<MessagingSettings>>().Value;

                            config.Host(rabbitMqSettings.Host, rabbitMqSettings.VirtualHost, h =>
                            {
                                h.Username(rabbitMqSettings.Username);
                                h.Password(rabbitMqSettings.Password);
                            });

                            config.ReceiveEndpoint(messagingSettings.QueueName, e =>
                            {
                                e.UseMessageRetry(cfg =>
                                {
                                    cfg.Exponential(
                                        retryLimit: 5,
                                        minInterval: TimeSpan.FromSeconds(2),
                                        maxInterval: TimeSpan.FromMinutes(2),
                                        intervalDelta: TimeSpan.FromSeconds(15));
                                });

                                e.Bind<SendConfirmationEmailRequestedEvent>();
                                e.ConfigureConsumer<SendConfirmationEmailRequestedEventConsumer>(context);

                                e.Bind<SendResetPasswordEmailRequestedEvent>();
                                e.ConfigureConsumer<SendResetPasswordEmailRequestedEventConsumer>(context);

                                e.Bind<SendSubmissionApprovedEmailRequestedEvent>();
                                e.ConfigureConsumer<SendSubmissionApprovedEmailRequestedEventConsumer>(context);

                                e.Bind<SendSubmissionRejectedEmailRequestedEvent>();
                                e.ConfigureConsumer<SendSubmissionRejectedEmailRequestedEventConsumer>(context);
                            });
                        });
                    });

                    var smtpSettings = context.Configuration.GetRequiredSection(SmtpSettings.SectionName).Get<SmtpSettings>();

                    services
                        .AddFluentEmail(smtpSettings!.From)
                        .AddRazorRenderer()
                        .AddMailKitSender(new SmtpClientOptions
                        {
                            Server = smtpSettings.Host,
                            Port = smtpSettings.Port,
                            User = smtpSettings.Username,
                            Password = smtpSettings.Password,
                            SocketOptions = smtpSettings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto,
                            RequiresAuthentication = true,
                            UseSsl = smtpSettings.EnableSsl,
                        });

                    services.AddScoped<IEmailSender, EmailSender>();
                });

            if (runAsService)
            {
                hostBuilder.UseWindowsService();
            }

            var host = hostBuilder.Build();

            host.Run();
        }
    }
}
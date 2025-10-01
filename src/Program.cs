using FluentEmail.MailKitSmtp;
using MailKit.Security;
using MassTransit;
using Microsoft.Extensions.Options;
using TagTheSpot.Services.Email.Consumers;
using TagTheSpot.Services.Email.Emails;
using TagTheSpot.Services.Email.Options;
using TagTheSpot.Services.Shared.Infrastructure.Options;
using TagTheSpot.Services.Shared.Messaging.Auth;
using TagTheSpot.Services.Shared.Messaging.Submissions;

namespace TagTheSpot.Services.Email
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            builder.Services.AddOptions<SmtpSettings>()
                .BindConfiguration(SmtpSettings.SectionName)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            builder.Services.AddOptions<RabbitMqSettings>()
                .BindConfiguration(RabbitMqSettings.SectionName)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            builder.Services.AddOptions<MessagingSettings>()
                .BindConfiguration(MessagingSettings.SectionName)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            builder.Services.AddMassTransit(cfg =>
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

            var smtpSettings = builder.Configuration.GetRequiredSection(SmtpSettings.SectionName).Get<SmtpSettings>();

            builder.Services
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

            builder.Services.AddScoped<IEmailSender, EmailSender>();

            var host = builder.Build();

            host.Run();
        }
    }
}
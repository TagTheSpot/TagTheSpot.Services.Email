using MassTransit;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using TagTheSpot.Services.Email.Consumers;
using TagTheSpot.Services.Email.Emails;
using TagTheSpot.Services.Email.Options;
using TagTheSpot.Services.Shared.Infrastructure.Options;
using TagTheSpot.Services.Shared.Messaging.Auth;

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
                        e.Bind<SendConfirmationEmailRequestedEvent>();
                        e.ConfigureConsumer<SendConfirmationEmailRequestedEventConsumer>(context);
                    });
                });
            });

            var smtpSettings = builder.Configuration.GetRequiredSection(SmtpSettings.SectionName).Get<SmtpSettings>();

            builder.Services
                .AddFluentEmail(smtpSettings!.From)
                .AddRazorRenderer()
                .AddSmtpSender(new SmtpClient(smtpSettings.Host)
                {
                    Port = smtpSettings.Port,
                    Credentials = new NetworkCredential(
                        userName: smtpSettings.Username,
                        password: smtpSettings.Password),
                    EnableSsl = smtpSettings.EnableSsl
                });

            builder.Services.AddSingleton<IEmailSender, EmailSender>();

            var host = builder.Build();

            host.Run();
        }
    }
}
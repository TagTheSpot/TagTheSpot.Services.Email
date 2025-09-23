using System.Net;
using System.Net.Mail;
using TagTheSpot.Services.Email.Emails;
using TagTheSpot.Services.Email.Options;
using TagTheSpot.Services.Shared.Messaging.Options;

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
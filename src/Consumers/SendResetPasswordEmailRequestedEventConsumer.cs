using MassTransit;
using TagTheSpot.Services.Email.Emails;
using TagTheSpot.Services.Email.Models;
using TagTheSpot.Services.Shared.Messaging.Auth;

namespace TagTheSpot.Services.Email.Consumers
{
    public sealed class SendResetPasswordEmailRequestedEventConsumer
        : IConsumer<SendResetPasswordEmailRequestedEvent>
    {
        private readonly IEmailSender _emailSender;

        public SendResetPasswordEmailRequestedEventConsumer(
            IEmailSender emailSender)
        {
            _emailSender = emailSender;
        }

        public async Task Consume(ConsumeContext<SendResetPasswordEmailRequestedEvent> context)
        {
            var message = context.Message;

            var request = new SendEmailRequest<ResetPasswordModel>(
                RecipientEmail: message.Recipient,
                Subject: "🔒 Відновлення пароля на TagTheSpot",
                new ResetPasswordModel(message.ResetPasswordLink));

            await _emailSender.SendEmailAsync(
                request,
                templatePath: "Templates/ResetPasswordTemplate.cshtml");
        }
    }
}

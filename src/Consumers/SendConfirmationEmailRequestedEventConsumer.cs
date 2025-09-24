using MassTransit;
using TagTheSpot.Services.Email.Emails;
using TagTheSpot.Services.Email.Models;
using TagTheSpot.Services.Shared.Messaging.Auth;

namespace TagTheSpot.Services.Email.Consumers
{
    public sealed class SendConfirmationEmailRequestedEventConsumer
        : IConsumer<SendConfirmationEmailRequestedEvent>
    {
        private readonly IEmailSender _emailSender;

        public SendConfirmationEmailRequestedEventConsumer(IEmailSender emailSender)
        {
            _emailSender = emailSender;
        }

        public async Task Consume(ConsumeContext<SendConfirmationEmailRequestedEvent> context)
        {
            var message = context.Message;

            var request = new SendEmailRequest<ConfirmEmailModel>(
                RecipientEmail: message.Recipient,
                Subject: "🔑 Завершіть реєстрацію на TagTheSpot!",
                new ConfirmEmailModel(message.ConfirmationLink));

            await _emailSender.SendEmailAsync(
                request,
                templatePath: "Templates/ConfirmEmailTemplate.cshtml");
        }
    }
}

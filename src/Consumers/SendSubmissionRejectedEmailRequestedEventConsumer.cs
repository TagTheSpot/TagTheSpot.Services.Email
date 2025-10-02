using MassTransit;
using TagTheSpot.Services.Email.Emails;
using TagTheSpot.Services.Shared.Messaging.Submissions;

namespace TagTheSpot.Services.Email.Consumers
{
    public sealed class SendSubmissionRejectedEmailRequestedEventConsumer
        : IConsumer<SendSubmissionRejectedEmailRequestedEvent>
    {
        private readonly IEmailSender _emailSender;

        public SendSubmissionRejectedEmailRequestedEventConsumer(
            IEmailSender emailSender)
        {
            _emailSender = emailSender;
        }

        public async Task Consume(ConsumeContext<SendSubmissionRejectedEmailRequestedEvent> context)
        {
            var message = context.Message;

            var request = new SendEmailRequest<SendSubmissionRejectedEmailRequestedEvent>(
                RecipientEmail: message.Recipient,
                Subject: "❌ Вашу заявку відхилено на TagTheSpot",
                message);

            var templatePath = Path.Combine(AppContext.BaseDirectory, "Templates", "SubmissionRejectedTemplate.cshtml");

            await _emailSender.SendEmailAsync(
                request,
                templatePath: templatePath);
        }
    }
}

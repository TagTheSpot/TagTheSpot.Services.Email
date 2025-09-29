using MassTransit;
using TagTheSpot.Services.Email.Emails;
using TagTheSpot.Services.Shared.Messaging.Submissions;

namespace TagTheSpot.Services.Email.Consumers
{
    public sealed class SendSubmissionApprovedEmailRequestedEventConsumer
        : IConsumer<SendSubmissionApprovedEmailRequestedEvent>
    {
        private readonly IEmailSender _emailSender;

        public SendSubmissionApprovedEmailRequestedEventConsumer(
            IEmailSender emailSender)
        {
            _emailSender = emailSender;
        }

        public async Task Consume(ConsumeContext<SendSubmissionApprovedEmailRequestedEvent> context)
        {
            var message = context.Message;

            var request = new SendEmailRequest<SendSubmissionApprovedEmailRequestedEvent>(
                RecipientEmail: message.Recipient,
                Subject: "🚀 Вашу заявку успішно підтверджено",
                message);

            await _emailSender.SendEmailAsync(
                request,
                templatePath: "Templates/SubmissionApprovedTemplate.cshtml");
        }
    }
}

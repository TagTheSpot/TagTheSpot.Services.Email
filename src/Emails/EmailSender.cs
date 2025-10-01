using FluentEmail.Core;
using FluentEmail.Core.Models;
using Polly;
using Polly.Retry;

namespace TagTheSpot.Services.Email.Emails
{
    internal sealed class EmailSender : IEmailSender
    {
        private readonly IFluentEmail _fluentEmail;
        private readonly AsyncRetryPolicy _retryPolicy;
        private readonly ILogger<EmailSender> _logger;

        public EmailSender(
            IFluentEmail fluentEmail,
            ILogger<EmailSender> logger)
        {
            _fluentEmail = fluentEmail;
            _logger = logger;

            _retryPolicy = BuildRetryPolicy(logger);
        }

        public async Task SendEmailAsync<TModel>(
            SendEmailRequest<TModel> request,
            string templateFileName)
        {
            await _retryPolicy.ExecuteAsync(async () =>
            {
                _logger.LogInformation(
                    "[EmailSender] Sending email to {Recipient} with subject '{Subject}'",
                    request.RecipientEmail,
                    request.Subject);

                var response = await _fluentEmail
                    .To(request.RecipientEmail)
                    .Subject(request.Subject)
                    .UsingTemplateFromFile(templateFileName, request.Model)
                    .SendAsync();

                if (!response.Successful)
                {
                    HandleFailure(request, response);
                }

                HandleSuccess(request);
            });
        }

        private void HandleSuccess<TModel>(SendEmailRequest<TModel> request)
        {
            _logger.LogInformation(
                "[EmailSender] Email successfully sent to {Recipient} with subject '{Subject}'",
                request.RecipientEmail,
                request.Subject);
        }

        private void HandleFailure<TModel>(SendEmailRequest<TModel> request, SendResponse response)
        {
            var errorDetails = string.Join(", ", response.ErrorMessages);

            _logger.LogError(
                "[EmailSender] Failed to send email to {Recipient}. Errors: {Errors}",
                request.RecipientEmail,
                errorDetails);

            throw new Exception($"Email sending failed: {errorDetails}");
        }

        private static AsyncRetryPolicy BuildRetryPolicy(ILogger logger)
        {
            return Policy
                .Handle<Exception>(ex =>
                    ex is System.Net.Mail.SmtpException ||
                    ex is TimeoutException)
                .WaitAndRetryAsync(
                    retryCount: 5,
                    attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                    (ex, ts, attempt, _) =>
                    {
                        logger.LogWarning(
                            ex,
                            "[EmailSender] Retry {Attempt} in {Delay}s due to: {Message}",
                            attempt,
                            ts.TotalSeconds,
                            ex.Message);
                    });
        }
    }
}

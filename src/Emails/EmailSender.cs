using FluentEmail.Core;

namespace TagTheSpot.Services.Email.Emails
{
    internal sealed class EmailSender : IEmailSender
    {
        private readonly IFluentEmail _fluentEmail;

        public EmailSender(IFluentEmail fluentEmail)
        {
            _fluentEmail = fluentEmail;
        }

        public async Task SendEmailAsync<TModel>(
            SendEmailRequest<TModel> request, 
            string templateFileName)
        {
            await _fluentEmail
                .To(request.RecipientEmail)
                .Subject(request.Subject)
                .UsingTemplateFromFile(templateFileName, request.Model)
                .SendAsync();
        }
    }
}

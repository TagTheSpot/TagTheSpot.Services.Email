namespace TagTheSpot.Services.Email.Emails
{
    public interface IEmailSender
    {
        Task SendEmailAsync<TModel>(
            SendEmailRequest<TModel> request,
            string templateFile);
    }
}

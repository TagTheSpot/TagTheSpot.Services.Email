namespace TagTheSpot.Services.Email.Emails
{
    public sealed record SendEmailRequest<TModel>(
        string RecipientEmail,
        string Subject,
        TModel Model);
}

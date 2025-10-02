using System.ComponentModel.DataAnnotations;
using TagTheSpot.Services.Shared.Abstractions.Options;

namespace TagTheSpot.Services.Email.Options
{
    public sealed class SmtpSettings : IAppOptions
    {
        public static string SectionName => nameof(SmtpSettings);

        [Required]
        public required string Username { get; init; }

        [Required]
        public required string Password { get; init; }

        [Required]
        public required string Host { get; init; }

        [Required]
        public required string From { get; init; }

        [Required]
        public int Port { get; init; }

        [Required]
        public bool EnableSsl { get; init; }
    }
}

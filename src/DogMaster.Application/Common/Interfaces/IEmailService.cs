namespace DogMaster.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendWelcomeAsync(string to, string firstName, string confirmationToken, CancellationToken ct = default);
    Task SendPasswordResetAsync(string to, string firstName, string resetToken, CancellationToken ct = default);
    Task SendPaymentConfirmedAsync(string to, string firstName, decimal amount, CancellationToken ct = default);
    Task SendCertificateAsync(string to, string firstName, string courseTitle, string certificateUrl, CancellationToken ct = default);
}

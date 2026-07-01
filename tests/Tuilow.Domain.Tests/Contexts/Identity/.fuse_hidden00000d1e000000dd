using DogMaster.Domain.Contexts.Identity.Entities;
using DogMaster.Domain.Contexts.Identity.Enums;
using FluentAssertions;
using Xunit;

namespace DogMaster.Domain.Tests.Contexts.Identity;

public sealed class UserTests
{
    [Fact]
    public void Register_WithValidData_ShouldCreateUserWithPendingStatus()
    {
        // Act
        var user = User.Register("teste@email.com", "SenhaSegura123!", "João", "Silva");

        // Assert
        user.Email.Value.Should().Be("teste@email.com");
        user.Profile.FirstName.Should().Be("João");
        user.Status.Should().Be(UserStatus.PendingConfirmation);
        user.IsEmailConfirmed.Should().BeFalse();
        user.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "UserRegisteredDomainEvent");
    }

    [Fact]
    public void Register_ShouldNormalizeEmailToLowercase()
    {
        var user = User.Register("TESTE@EMAIL.COM", "SenhaSegura123!", "João", "Silva");
        user.Email.Value.Should().Be("teste@email.com");
    }

    [Fact]
    public void ConfirmEmail_WithValidToken_ShouldActivateUser()
    {
        var user = User.Register("teste@email.com", "SenhaSegura123!", "João", "Silva");
        var token = user.EmailConfirmationToken!;

        user.ConfirmEmail(token);

        user.IsEmailConfirmed.Should().BeTrue();
        user.Status.Should().Be(UserStatus.Active);
    }

    [Fact]
    public void ConfirmEmail_WithInvalidToken_ShouldThrow()
    {
        var user = User.Register("teste@email.com", "SenhaSegura123!", "João", "Silva");

        var act = () => user.ConfirmEmail("token-invalido");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ValidatePassword_WithCorrectPassword_ShouldReturnTrue()
    {
        var user = User.Register("teste@email.com", "SenhaSegura123!", "João", "Silva");
        user.ValidatePassword("SenhaSegura123!").Should().BeTrue();
    }

    [Fact]
    public void ValidatePassword_WithWrongPassword_ShouldReturnFalse()
    {
        var user = User.Register("teste@email.com", "SenhaSegura123!", "João", "Silva");
        user.ValidatePassword("SenhaErrada").Should().BeFalse();
    }

    [Fact]
    public void AddRefreshToken_ShouldRevokeExistingActiveTokens()
    {
        var user = User.Register("teste@email.com", "SenhaSegura123!", "João", "Silva");
        user.AddRefreshToken("token1", DateTime.UtcNow.AddDays(30));
        user.AddRefreshToken("token2", DateTime.UtcNow.AddDays(30));

        var active = user.RefreshTokens.Where(r => r.IsActive).ToList();
        active.Should().HaveCount(1);
        active[0].Token.Should().Be("token2");
    }

    [Fact]
    public void RequestPasswordReset_ShouldSetTokenWithExpiry()
    {
        var user = User.Register("teste@email.com", "SenhaSegura123!", "João", "Silva");
        var token = user.RequestPasswordReset();

        token.Should().NotBeNullOrEmpty();
        user.PasswordResetTokenExpiry.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public void ResetPassword_WithValidToken_ShouldUpdatePassword()
    {
        var user = User.Register("teste@email.com", "SenhaAntiga123!", "João", "Silva");
        var token = user.RequestPasswordReset();

        user.ResetPassword(token, "NovaSenha456!");

        user.ValidatePassword("NovaSenha456!").Should().BeTrue();
        user.ValidatePassword("SenhaAntiga123!").Should().BeFalse();
    }
}

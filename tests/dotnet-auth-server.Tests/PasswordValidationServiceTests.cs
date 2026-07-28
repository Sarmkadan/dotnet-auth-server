using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Moq;
using DotnetAuthServer.Configuration;
using DotnetAuthServer.Exceptions;
using DotnetAuthServer.Services;

namespace dotnet_auth_server.Tests
{
    public class PasswordValidationServiceTests
    {
        [Fact]
        public void ValidatePassword_HappyPath_NoErrors()
        {
            // Arrange
            var authServerOptions = new AuthServerOptions();
            var passwordPolicyOptions = new PasswordPolicyOptions();
            var passwordValidationService = new PasswordValidationService(authServerOptions, passwordPolicyOptions);
            var password = "P@ssw0rd";

            // Act
            var errors = passwordValidationService.ValidatePassword(password);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public void ValidatePassword_EdgeCase_NullPassword()
        {
            // Arrange
            var authServerOptions = new AuthServerOptions();
            var passwordPolicyOptions = new PasswordPolicyOptions();
            var passwordValidationService = new PasswordValidationService(authServerOptions, passwordPolicyOptions);
            string? password = null;

            // Act and Assert
            Assert.Throws<ArgumentNullException>(() => passwordValidationService.ValidatePassword(password!));
        }

        [Fact]
        public void ValidateAndThrow_HappyPath_NoException()
        {
            // Arrange
            var authServerOptions = new AuthServerOptions();
            var passwordPolicyOptions = new PasswordPolicyOptions();
            var passwordValidationService = new PasswordValidationService(authServerOptions, passwordPolicyOptions);
            var password = "P@ssw0rd";

            // Act and Assert
            passwordValidationService.ValidateAndThrow(password);
        }

        [Fact]
        public void ValidateAndThrow_ErrorPath_InvalidPassword()
        {
            // Arrange
            var authServerOptions = new AuthServerOptions();
            var passwordPolicyOptions = new PasswordPolicyOptions();
            passwordPolicyOptions.RequireLowercase = true;
            passwordPolicyOptions.RequireUppercase = true;
            passwordPolicyOptions.RequireDigit = true;
            var passwordValidationService = new PasswordValidationService(authServerOptions, passwordPolicyOptions);
            var password = "invalid";

            // Act and Assert
            Assert.Throws<AuthServerException>(() => passwordValidationService.ValidateAndThrow(password));
        }
    }
}

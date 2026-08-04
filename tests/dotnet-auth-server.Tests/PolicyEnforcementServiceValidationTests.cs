using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using DotnetAuthServer.Services;
using FluentAssertions;
using Xunit;

namespace DotnetAuthServer.Tests.Services
{
    public class PolicyEnforcementServiceValidationTests
    {
        // Helper to create an instance without invoking its constructor.
        private static PolicyEnforcementService CreateServiceInstance()
        {
            return (PolicyEnforcementService)FormatterServices.GetUninitializedObject(typeof(PolicyEnforcementService));
        }

        #region PolicyEnforcementService validation

        [Fact]
        public void Validate_Service_ReturnsEmptyList_WhenInstanceIsNotNull()
        {
            // Arrange
            var service = CreateServiceInstance();

            // Act
            var result = service.Validate();

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void IsValid_Service_ReturnsTrue_WhenInstanceIsNotNull()
        {
            // Arrange
            var service = CreateServiceInstance();

            // Act
            var isValid = service.IsValid();

            // Assert
            isValid.Should().BeTrue();
        }

        [Fact]
        public void EnsureValid_Service_DoesNotThrow_WhenInstanceIsValid()
        {
            // Arrange
            var service = CreateServiceInstance();

            // Act
            Action act = () => service.EnsureValid();

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void Validate_Service_ThrowsArgumentNullException_WhenInstanceIsNull()
        {
            // Arrange
            PolicyEnforcementService? service = null;

            // Act
            Action act = () => service!.Validate();

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        #endregion

        #region Policy validation

        private static Policy CreateValidPolicy()
        {
            return new Policy
            {
                Rules = new List<PolicyRule>
                {
                    new PolicyRule
                    {
                        Type = default, // default enum value is a defined value
                        Match = default,
                        Attribute = "some-attr",
                        Values = new List<string> { "value1" }
                    }
                },
                CombineWith = default // default enum value is defined
            };
        }

        [Fact]
        public void Validate_Policy_ReturnsEmpty_WhenPolicyIsValid()
        {
            // Arrange
            var policy = CreateValidPolicy();

            // Act
            var result = policy.Validate();

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void Validate_Policy_ReturnsError_WhenRulesIsNull()
        {
            // Arrange
            var policy = new Policy
            {
                Rules = null,
                CombineWith = default
            };

            // Act
            var result = policy.Validate();

            // Assert
            result.Should().ContainSingle()
                  .Which.Should().Contain("Policy.Rules cannot be null");
        }

        [Fact]
        public void Validate_Policy_ReturnsError_WhenRulesIsEmpty()
        {
            // Arrange
            var policy = new Policy
            {
                Rules = new List<PolicyRule>(),
                CombineWith = default
            };

            // Act
            var result = policy.Validate();

            // Assert
            result.Should().ContainSingle()
                  .Which.Should().Contain("Policy.Rules must contain at least one rule");
        }

        [Fact]
        public void Validate_Policy_ReturnsError_WhenCombineWithIsInvalid()
        {
            // Arrange
            var policy = new Policy
            {
                Rules = new List<PolicyRule>
                {
                    new PolicyRule
                    {
                        Type = default,
                        Match = default,
                        Attribute = "attr",
                        Values = new List<string> { "v" }
                    }
                },
                // Cast an undefined value to the enum
                CombineWith = (PolicyCombineMode)999
            };

            // Act
            var result = policy.Validate();

            // Assert
            result.Should().ContainSingle()
                  .Which.Should().Contain("Policy.CombineWith has invalid value");
        }

        [Fact]
        public void IsValid_Policy_ReturnsTrue_WhenValid()
        {
            // Arrange
            var policy = CreateValidPolicy();

            // Act
            var isValid = policy.IsValid();

            // Assert
            isValid.Should().BeTrue();
        }

        [Fact]
        public void IsValid_Policy_ReturnsFalse_WhenInvalid()
        {
            // Arrange
            var policy = new Policy
            {
                Rules = null,
                CombineWith = default
            };

            // Act
            var isValid = policy.IsValid();

            // Assert
            isValid.Should().BeFalse();
        }

        [Fact]
        public void EnsureValid_Policy_ThrowsArgumentException_WhenInvalid()
        {
            // Arrange
            var policy = new Policy
            {
                Rules = null,
                CombineWith = default
            };

            // Act
            Action act = () => policy.EnsureValid();

            // Assert
            act.Should().Throw<ArgumentException>()
               .WithMessage("*Policy validation failed*");
        }

        #endregion

        #region PolicyRule validation

        private static PolicyRule CreateValidRule()
        {
            return new PolicyRule
            {
                Type = default,
                Match = default,
                Attribute = "attr",
                Values = new List<string> { "value" }
            };
        }

        [Fact]
        public void Validate_Rule_ReturnsEmpty_WhenRuleIsValid()
        {
            // Arrange
            var rule = CreateValidRule();

            // Act
            var result = rule.Validate();

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void Validate_Rule_ReturnsError_WhenTypeIsInvalid()
        {
            // Arrange
            var rule = new PolicyRule
            {
                Type = (PolicyRuleType)999,
                Match = default,
                Attribute = "attr",
                Values = new List<string> { "v" }
            };

            // Act
            var result = rule.Validate();

            // Assert
            result.Should().ContainSingle()
                  .Which.Should().Contain("PolicyRule.Type has invalid value");
        }

        [Fact]
        public void Validate_Rule_ReturnsError_WhenMatchIsInvalid()
        {
            // Arrange
            var rule = new PolicyRule
            {
                Type = default,
                Match = (PolicyMatchMode)999,
                Attribute = "attr",
                Values = new List<string> { "v" }
            };

            // Act
            var result = rule.Validate();

            // Assert
            result.Should().ContainSingle()
                  .Which.Should().Contain("PolicyRule.Match has invalid value");
        }

        [Fact]
        public void Validate_Rule_ReturnsError_WhenAttributeMissingForAttributeType()
        {
            // Arrange
            var rule = new PolicyRule
            {
                Type = PolicyRuleType.Attribute,
                Match = default,
                Attribute = null,
                Values = new List<string> { "v" }
            };

            // Act
            var result = rule.Validate();

            // Assert
            result.Should().ContainSingle()
                  .Which.Should().Contain("PolicyRule.Attribute is required");
        }

        [Fact]
        public void Validate_Rule_ReturnsError_WhenValuesIsNull()
        {
            // Arrange
            var rule = new PolicyRule
            {
                Type = default,
                Match = default,
                Attribute = "attr",
                Values = null
            };

            // Act
            var result = rule.Validate();

            // Assert
            result.Should().ContainSingle()
                  .Which.Should().Contain("PolicyRule.Values cannot be null");
        }

        [Fact]
        public void EnsureValid_Rule_ThrowsArgumentException_WhenInvalid()
        {
            // Arrange
            var rule = new PolicyRule
            {
                Type = PolicyRuleType.Attribute,
                Match = default,
                Attribute = null,
                Values = new List<string> { "v" }
            };

            // Act
            Action act = () => rule.EnsureValid();

            // Assert
            act.Should().Throw<ArgumentException>()
               .WithMessage("*PolicyRule validation failed*");
        }

        #endregion

        #region String (policy name) validation

        [Fact]
        public void Validate_PolicyName_ReturnsEmpty_WhenValid()
        {
            // Arrange
            string name = "valid-policy-name";

            // Act
            var result = name.Validate();

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void Validate_PolicyName_ThrowsArgumentNullException_WhenNull()
        {
            // Arrange
            string? name = null;

            // Act
            Action act = () => name!.Validate();

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Validate_PolicyName_ReturnsError_WhenWhitespace()
        {
            // Arrange
            string name = "   ";

            // Act
            var result = name.Validate();

            // Assert
            result.Should().ContainSingle()
                  .Which.Should().Contain("cannot be null or whitespace");
        }

        [Fact]
        public void Validate_PolicyName_ReturnsError_WhenTooLong()
        {
            // Arrange
            string name = new string('a', 101); // 101 > 100

            // Act
            var result = name.Validate();

            // Assert
            result.Should().ContainSingle()
                  .Which.Should().Contain("cannot exceed 100 characters");
        }

        [Fact]
        public void EnsureValid_PolicyName_ThrowsArgumentException_WhenInvalid()
        {
            // Arrange
            string name = "";

            // Act
            Action act = () => name.EnsureValid();

            // Assert
            act.Should().Throw<ArgumentException>()
               .WithMessage("*cannot be null or whitespace*");
        }

        #endregion
    }
}

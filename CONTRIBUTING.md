# Contributing to dotnet-auth-server

Thank you for your interest in contributing! This document provides guidelines and instructions for contributing to the project.

## Table of Contents

1. [Code of Conduct](#code-of-conduct)
2. [Getting Started](#getting-started)
3. [Development Workflow](#development-workflow)
4. [Code Style](#code-style)
5. [Testing](#testing)
6. [Commit Messages](#commit-messages)
7. [Pull Requests](#pull-requests)
8. [Reporting Issues](#reporting-issues)

---

## Code of Conduct

- Be respectful and inclusive
- Welcome all skill levels
- Assume good intentions
- Focus on the code, not the person
- Help others learn and grow

---

## Getting Started

### Prerequisites

- .NET 10.0 SDK ([Download](https://dotnet.microsoft.com/download/dotnet/10.0))
- Visual Studio 2025, VS Code, or compatible IDE
- Git for version control
- Make (optional, for Makefile commands)

### Clone & Setup

```bash
# Clone repository
git clone https://github.com/Sarmkadan/dotnet-auth-server.git
cd dotnet-auth-server

# Restore dependencies
dotnet restore

# Build project
dotnet build

# Run tests
dotnet test

# Run locally
dotnet run
```

---

## Development Workflow

### 1. Create a Feature Branch

```bash
git checkout -b feature/your-feature-name
```

Branch naming convention:
- `feature/` - New features
- `fix/` - Bug fixes
- `docs/` - Documentation
- `refactor/` - Code refactoring
- `test/` - Tests
- `perf/` - Performance improvements

### 2. Make Changes

- Keep commits small and focused
- One logical change per commit
- Update related documentation
- Add tests for new features

### 3. Test Locally

```bash
# Run all tests
dotnet test

# Run specific test
dotnet test --filter "TestClass"

# Run with coverage
dotnet test /p:CollectCoverage=true
```

### 4. Format Code

```bash
# Format entire codebase
dotnet format

# Specific file
dotnet format path/to/file.cs
```

### 5. Commit Changes

```bash
git add .
git commit -m "[TYPE] Brief description"
```

See [Commit Messages](#commit-messages) section for guidelines.

### 6. Push & Create PR

```bash
git push origin feature/your-feature-name
```

Then create a Pull Request on GitHub.

---

## Code Style

### C# Conventions

- **Naming**: PascalCase for public members, camelCase for locals
- **Indentation**: 4 spaces (configured in .editorconfig)
- **Line Length**: 120 characters (soft limit)
- **Null Safety**: Enable nullable reference types (`#nullable enable`)
- **Comments**: Only for non-obvious logic (the "why", not the "what")

### Example

```csharp
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Services;

/// <summary>
/// Service for managing user authentication and session state.
/// </summary>
public class AuthService
{
    private readonly IUserRepository _userRepository;

    public AuthService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    /// <summary>
    /// Authenticate user with credentials.
    /// </summary>
    /// <returns>User if credentials valid; null otherwise</returns>
    public async Task<User?> AuthenticateAsync(string username, string password)
    {
        var user = await _userRepository.FindByUsernameAsync(username);

        if (user == null)
            return null;

        // Constant-time comparison prevents timing attacks
        var isPasswordValid = BCrypt.Verify(password, user.PasswordHash);

        return isPasswordValid ? user : null;
    }
}
```

### Code Review Checklist

- [ ] Follows naming conventions
- [ ] Code is readable and maintainable
- [ ] Comments explain "why", not "what"
- [ ] No unnecessary complexity
- [ ] Follows SOLID principles
- [ ] No code duplication (DRY)
- [ ] Proper error handling
- [ ] No hardcoded values (use configuration)
- [ ] Security best practices followed
- [ ] Performance considered

---

## Testing

### Test Structure

Tests should follow the Arrange-Act-Assert (AAA) pattern:

```csharp
[Fact]
public async Task GenerateToken_WithValidRequest_ReturnsAccessToken()
{
    // Arrange
    var user = new User { UserId = "user1", Email = "test@example.com" };
    var client = new Client { ClientId = "client1" };
    var scopes = new[] { "openid", "profile" };

    // Act
    var token = await _tokenService.GenerateTokenAsync(user, client, scopes);

    // Assert
    Assert.NotNull(token);
    Assert.NotEmpty(token.AccessToken);
    Assert.Equal(3600, token.ExpiresIn);
}
```

### Test Naming

`[Method]_[Scenario]_[ExpectedResult]`

Examples:
- `GenerateToken_WithValidScopes_ReturnsJwtToken`
- `AuthenticateUser_WithInvalidPassword_ReturnsNull`
- `ValidateClient_WithBlacklistedClient_ReturnsFalse`

### Coverage Goals

- Aim for 80%+ code coverage
- Test happy path, edge cases, and error scenarios
- Test integration between components
- Test security-relevant code thoroughly

### Running Tests

```bash
# All tests
dotnet test

# Specific project
dotnet test src/DotnetAuthServer.Tests

# Specific class
dotnet test --filter "ClassName"

# With coverage
dotnet test /p:CollectCoverage=true /p:CoverageFormat=opencover
```

---

## Commit Messages

### Format

```
[TYPE] Brief description (under 70 chars)

Longer explanation if needed. Explain WHY the change was made.
Include relevant context and trade-offs.

- Point 1
- Point 2

Fixes #123
```

### Types

- **feat**: New feature
- **fix**: Bug fix
- **docs**: Documentation changes
- **test**: Test additions/changes
- **refactor**: Code refactoring
- **perf**: Performance improvement
- **security**: Security fix
- **ci**: CI/CD changes
- **chore**: Build, deps, config changes

### Examples

**Good:**
```
[feat] Add token introspection endpoint

Implements RFC 7662 token introspection to allow resource
servers to validate tokens. Includes token active status,
claims, and expiration info.

- Validates token signature and expiration
- Returns standardized error responses
- Caches introspection results (1 minute)

Fixes #45
```

**Bad:**
```
fixed stuff
Updated code
WIP
```

---

## Pull Requests

### PR Title

- Clear and descriptive
- Start with [TYPE] prefix (feat, fix, docs, etc.)
- Under 70 characters
- Example: `[feat] Add device authorization flow`

### PR Description

```markdown
## Description
Brief summary of the change.

## Type
- [ ] New feature
- [ ] Bug fix
- [ ] Documentation
- [ ] Performance
- [ ] Security

## Related Issues
Fixes #123

## Testing
- [ ] Unit tests added
- [ ] Integration tests added
- [ ] Manual testing completed

## Checklist
- [ ] Code follows style guidelines
- [ ] Self-review completed
- [ ] Comments added for complex logic
- [ ] Documentation updated
- [ ] No breaking changes
- [ ] Tests pass locally
```

### Review Process

1. **Author** creates PR with descriptive information
2. **Reviewers** examine code for quality, correctness, security
3. **CI/CD** runs automated tests and checks
4. **Feedback** is addressed with new commits
5. **Approval** from maintainers before merge
6. **Merge** to main/develop branch

### Expectations

- Respond to feedback constructively
- Keep discussion professional and focused
- Don't take criticism personally
- Help reviewers understand your changes

---

## Reporting Issues

### Bug Reports

Include:
- Clear description of the problem
- Steps to reproduce
- Expected vs actual behavior
- Environment (OS, .NET version, etc.)
- Error message and stack trace
- Code sample if applicable

**Template:**
```markdown
## Description
Clear description of the bug.

## Steps to Reproduce
1. ...
2. ...
3. ...

## Expected Behavior
What should happen.

## Actual Behavior
What actually happens.

## Environment
- OS: Windows 10
- .NET: 10.0
- Browser: Chrome 120

## Error
Stack trace or error message.
```

### Feature Requests

Include:
- Problem statement (what problem does it solve?)
- Proposed solution
- Alternative approaches considered
- Example use case

---

## Questions?

- **GitHub Issues**: For bug reports and feature requests
- **Discussions**: For questions and ideas
- **Email**: rutova2@gmail.com for sensitive matters

---

## Recognition

Contributors are recognized in:
- `CHANGELOG.md` release notes
- GitHub contributors page
- Project documentation

Thank you for contributing to dotnet-auth-server! 🎉

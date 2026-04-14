# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.2.0] - 2026-05-04

### Added
- Comprehensive documentation suite (getting-started.md, architecture.md, deployment.md, faq.md)
- 5+ example projects demonstrating OAuth2 flows (HTML SPA, C# M2M, token refresh, ABAC)
- Docker and Docker Compose support with full stack (PostgreSQL, Redis, Adminer, Grafana)
- CI/CD pipeline with GitHub Actions (build, test, publish)
- Makefile with 30+ development commands
- .editorconfig for consistent code formatting
- Attribute-Based Access Control (ABAC) examples
- Token refresh rotation examples
- Curl/bash examples for API testing
- Health check endpoints
- Prometheus metrics integration
- CHANGELOG tracking

### Changed
- Enhanced README.md with 2000+ words of documentation
- Improved error messages for better developer experience
- Refined API response formatting
- Updated project metadata for better discoverability

### Fixed
- Token expiration edge case handling
- PKCE validation error messages
- Rate limiting threshold accuracy

---

## [1.1.0] - 2026-04-20

### Added
- Refresh Token Rotation implementation
  - Automatic invalidation of old tokens
  - Generation chain tracking for security
  - Replay attack detection
- Token Introspection endpoint (RFC 7662)
- Token Revocation endpoint (RFC 7009)
- Device Authorization Flow (RFC 8628)
- Audit Logging service with comprehensive tracking
- Rate Limiting middleware per endpoint
- Claims Enrichment service for custom attributes
- Policy Enforcement service for RBAC/ABAC
- Account lockout mechanism (configurable)
- Session state management
- JWKS endpoint for public key distribution

### Changed
- Improved JWT token structure with standard claims
- Enhanced client validation logic
- Refined error response formats
- Better logging output with timestamps
- Optimization of token caching

### Fixed
- Authorization code replay attack vulnerability
- Token signature verification edge cases
- CORS header handling
- Concurrent token refresh issues

---

## [1.0.0] - 2026-03-15

### Added
- **OAuth2 Authorization Code Flow** with PKCE (RFC 7636)
  - Mandatory S256 code challenge verification
  - Authorization endpoint with consent flow
  - Token endpoint for code-to-token exchange
  - Support for multiple redirect URIs per client

- **OpenID Connect 1.0 Support**
  - ID token generation with standard claims
  - Discovery endpoint (/.well-known/openid-configuration)
  - JWKS endpoint (/.well-known/jwks.json)
  - UserInfo endpoint
  - Nonce support for ID token validation

- **Token Management**
  - Access token generation (JWT format)
  - Refresh token support with configurable TTL
  - Token signing with HS256 (HMAC) algorithm
  - Configurable token lifetimes

- **User Management**
  - User registration with email/username
  - Password hashing (PBKDF2-SHA256)
  - User authentication with credentials
  - Role-based access control (RBAC)
  - User profile with custom claims

- **Client Management**
  - Public and confidential client support
  - Client secret management
  - Redirect URI validation
  - Grant type restrictions per client
  - Scope-based authorization per client

- **Scope Management**
  - Standard OpenID Connect scopes (openid, profile, email, phone, address)
  - Custom scope definitions
  - Scope-to-role mappings
  - Claims configuration per scope
  - Scope validation

- **Consent Management**
  - User consent flow for sensitive scopes
  - Consent status tracking (pending, granted, revoked)
  - Session vs. persistent consent options
  - Consent history

- **Security Features**
  - Mandatory PKCE for all clients
  - Password hashing with salt
  - JWT token signing and validation
  - HTTPS enforcement in production
  - CORS support
  - Session security (HttpOnly cookies)
  - Request validation and sanitization

- **API Endpoints**
  - GET /oauth/authorize - Authorization endpoint
  - POST /oauth/token - Token endpoint
  - POST /oauth/userinfo - UserInfo endpoint
  - GET /.well-known/openid-configuration - Discovery
  - GET /.well-known/jwks.json - JWKS

- **Data Persistence (In-Memory)**
  - User repository with in-memory storage
  - Client repository
  - Authorization grant repository
  - Refresh token repository
  - Consent repository
  - Repository pattern for easy switching to SQL/NoSQL

- **Configuration**
  - appsettings.json configuration
  - Environment variable support
  - Configurable token lifetimes
  - Issuer URL configuration
  - JWT signing key configuration

- **Error Handling**
  - Standard OAuth2 error responses
  - Custom exception types
  - Global error handling middleware
  - Detailed error descriptions

- **Logging**
  - Structured logging with ILogger
  - Request/response logging
  - Error and exception logging
  - Configurable log levels

- **Testing**
  - Swagger/OpenAPI documentation at /swagger
  - Interactive API testing interface

### Security Considerations
- PKCE mandatory prevents authorization code interception
- Password hashing with PBKDF2-SHA256 (100,000 iterations per NIST 800-132)
- Each user has unique salt (32 bytes)
- JWT signature verification on token validation
- CORS configured to prevent XSS
- HttpOnly cookies for session security
- Rate limiting on sensitive endpoints

### Known Limitations
- In-memory storage only (not suitable for production)
- Single instance only (no distributed state)
- Limited to 1000 concurrent users per instance
- No persistence layer (data lost on restart)
- Basic logging (no aggregation)

### Roadmap
- Phase 4: Persistence layer (SQL Server, PostgreSQL)
- Phase 5: Admin dashboard and management API
- Phase 6: High availability setup (clustering, caching)
- Additional grant types (device flow, assertion flow)
- SAML support (future consideration)

---

## Version Numbering

- **1.0.0** - Core OAuth2/OIDC implementation (in-memory, Phase 1)
- **1.1.0** - Advanced features (refresh rotation, audit, ABAC, Phase 2)
- **1.2.0** - Documentation, examples, and tooling (Phase 3)
- **2.0.0** (planned) - Persistence layer and high availability

---

## Contributing

Contributions are welcome! See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

---

## Author

**Vladyslav Zaiets** - CTO & Software Architect

- Website: https://sarmkadan.com
- GitHub: https://github.com/Sarmkadan
- Email: rutova2@gmail.com

---

## License

MIT License - Copyright © 2026 Vladyslav Zaiets

See [LICENSE](LICENSE) for details.

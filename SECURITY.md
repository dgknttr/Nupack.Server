# Security Policy

## Supported Versions

We actively support the following versions of Nupack Server with security updates:

| Version | Supported          |
| ------- | ------------------ |
| 1.0.x   | :white_check_mark: |
| < 1.0   | :x:                |

## Reporting a Vulnerability

We take the security of Nupack Server seriously. If you believe you have found a security vulnerability, please report it to us as described below.

### How to Report

**Please do not report security vulnerabilities through public GitHub issues.**

Instead, please report security vulnerabilities using one of the following methods:

1. **GitHub Security Advisories** (Preferred): Use the Security tab in the repository
2. **Email**: Contact the maintainers directly (see repository for current contact information)

Include the following information in your report:

- **Type of issue** (e.g. buffer overflow, SQL injection, cross-site scripting, etc.)
- **Full paths of source file(s)** related to the manifestation of the issue
- **The location of the affected source code** (tag/branch/commit or direct URL)
- **Any special configuration** required to reproduce the issue
- **Step-by-step instructions** to reproduce the issue
- **Proof-of-concept or exploit code** (if possible)
- **Impact of the issue**, including how an attacker might exploit the issue

### What to Expect

- **Acknowledgment**: We will acknowledge receipt of your vulnerability report within 48 hours.
- **Initial Assessment**: We will provide an initial assessment within 5 business days.
- **Regular Updates**: We will keep you informed of our progress throughout the process.
- **Resolution Timeline**: We aim to resolve critical vulnerabilities within 30 days.

### Disclosure Policy

- **Coordinated Disclosure**: We follow a coordinated disclosure process.
- **Public Disclosure**: Vulnerabilities will be publicly disclosed after a fix is available.
- **Credit**: We will credit security researchers who responsibly disclose vulnerabilities.

## Security Best Practices

### For Deployment

1. **Use HTTPS**: Always deploy behind a reverse proxy with TLS/SSL termination
2. **Authentication**: Implement authentication for package uploads in production
3. **Network Security**: Restrict network access to trusted sources
4. **Regular Updates**: Keep the server and dependencies updated
5. **Monitoring**: Implement logging and monitoring for security events

### For Development

1. **Input Validation**: All user inputs are validated and sanitized
2. **File Upload Security**: Only .nupkg files are accepted with size limits
3. **Error Handling**: Sensitive information is not exposed in error messages
4. **Dependencies**: Regular security scanning of NuGet dependencies

## Security Features

### Current Implementation

- ✅ **Input Validation**: Strict validation of uploaded packages
- ✅ **File Type Validation**: Only .nupkg files are accepted
- ✅ **Size Limits**: Configurable upload size limits
- ✅ **Error Handling**: Secure error responses without information leakage
- ✅ **Logging**: Comprehensive security event logging
- ✅ **Container Security**: Non-root Docker container execution

### Recommended for Production

- 🔄 **Authentication**: API key or OAuth-based authentication
- 🔄 **Authorization**: Role-based access control
- 🔄 **Rate Limiting**: Protection against abuse
- 🔄 **HTTPS**: TLS encryption for all communications
- 🔄 **WAF**: Web Application Firewall protection
- 🔄 **Backup Encryption**: Encrypted backup storage

## Known Security Considerations

### File System Storage

- **Risk**: Direct file system access
- **Mitigation**: Proper file permissions and access controls
- **Recommendation**: Consider cloud storage for enhanced security

### Package Validation

- **Risk**: Malicious package content
- **Mitigation**: Package signature validation (future enhancement)
- **Recommendation**: Implement package scanning in CI/CD pipeline

### Web UI

- **Risk**: XSS vulnerabilities in package metadata display
- **Mitigation**: HTML encoding and CSP headers
- **Status**: Implemented in current version

## Compliance

This project follows security best practices including:

- **OWASP Top 10**: Protection against common web vulnerabilities
- **NIST Guidelines**: Secure software development practices
- **Industry Standards**: Following .NET security recommendations

## Security Updates

Security updates will be:

- **Prioritized**: Critical security fixes take precedence
- **Documented**: Listed in CHANGELOG.md with CVE references
- **Communicated**: Announced through GitHub releases and security advisories

## Contact

For security-related questions or concerns:

- **GitHub Security**: Use the Security tab in the repository
- **Issues**: For non-sensitive security questions, open a GitHub issue with the `security` label
- **Response Time**: We aim to acknowledge security reports within 48 hours

## Acknowledgments

We thank the security research community for helping keep Nupack Server secure. Responsible disclosure helps protect all users.

---

**Last Updated**: December 2024
**Next Review**: March 2025

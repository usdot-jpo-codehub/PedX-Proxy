# Changelog

All notable changes to the PedX Proxy project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-06-09

### Added
- Initial release of the PedX Proxy
- Support for MaxTime intersection controllers
- REST API for querying intersection information
- REST API for requesting pedestrian crossings
- API key authentication with role-based access control
- Comprehensive logging with Serilog
- Configuration system with appsettings.json, security.json, and intersections.json
- Documentation including README, LICENSE, and CONTRIBUTING guidelines
- Docker support for containerized deployment
- Cross-platform support (.NET 8.0)

### Security
- API key authentication for all endpoints
- Role-based access control (reader and caller roles)
- HTTPS support with TLS

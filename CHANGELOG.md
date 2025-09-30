# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Authentication endpoints: `POST /auth/register`, `POST /auth/login`, `GET /auth/me`.
- Password hashing with PBKDF2.
- JWT token generation and validation.
- Swagger with JWT support (enabled only in development).
- Infrastructure seeding for development environment.
- Docker setup for dev and prod with `compose.yaml`.
- Scripts (`run.js`, `dev.*`, `prod.*`, `test.*`) for unified workflows.
- GitHub Actions CI with unit and integration tests.

# collab-task

Collaborative real-time task management app built with **ASP.NET Core** (backend) and **React** (frontend).

---

## Testing

The solution includes two levels of automated tests:

- **Unit tests (fast, no external infra)**  
  Cover **Domain**, **Application**, and **API** surface (minimal endpoints, filters, problem details) using in-memory doubles and test host.  
  No database or external services are required.

- **Integration tests (with infrastructure)**  
  Validate the **Infrastructure** layer using [Testcontainers for .NET](https://github.com/testcontainers/testcontainers-dotnet) with SQL Server to exercise EF Core persistence, migrations, idempotent seeding, and concurrency.
  
### Run tests

```
npm run test:unit     # run unit tests only (Domain, Application, API)
npm run test:infra    # run infrastructure tests with Testcontainers
npm run test:all      # run full test suite (unit + integration)
```

### Notes

- Integration tests require Docker running locally.
- Each infrastructure test suite spins up an ephemeral SQL Server container.
- Tests are isolated and leave no state behind.
- Test coverage thresholds are configured in `Directory.Build.props`.

---

### Continuous Integration

- CI is configured via GitHub Actions (`.github/workflows/ci.yml`).
- On every push or pull request:
  - Build against .NET 8.
  - Run unit and integration tests (with Docker on hosted runners).
- The workflow fails if any test fails or coverage drops below the configured threshold.
 
---

### Local Development

- Requires **.NET 8 SDK**, **Node.js 20+**, and **Docker Desktop** (for integration tests).
```
npm run dev   # docker-compose up for development
npm run prod  # docker-compose up for production profile
```
- Backend and frontend can be run independently or together via the `infra/` compose files.

---

### Developer utilities

```
npm run gen:api         # generate TypeScript types from OpenAPI (web/src/shared/api/types.ts)
npm run check:contract  # validate OpenAPI contract vs expected invariants
```

---
### License

This project is licensed under the MIT License. See the [LICENSE](./LICENSE) file for details.

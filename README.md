# collab-task

Collaborative real-time task management app built with **ASP.NET Core** (backend) and **React** (frontend).

---

## Testing

The solution includes two categories of automated tests:

- **Unit tests**  
  Validate **Domain** and **Application** layers in isolation.  
  They run fast and without infrastructure dependencies.

- **Integration tests**  
  Validate the **Infrastructure** layer using [Testcontainers for .NET](https://github.com/testcontainers/testcontainers-dotnet) with SQL Server.
  
  These tests cover:
  - EF Core persistence
  - Database migrations
  - Idempotent seeding
  - Concurrency handling

---

### Run tests (Linux/macOS)

```
./scripts/test-unit.sh      # run unit tests only
./scripts/test-infra.sh     # run infrastructure tests with Testcontainers
dotnet test                 # run full test suite (unit + integration)
```

### Run tests (Windows Powershell)

```
.\scripts\test-unit.ps1     # run unit tests only
.\scripts\test-infra.ps1    # run infrastructure tests with Testcontainers
dotnet test                 # run full test suite (unit + integration)
```

---

### Notes

- Integration tests require Docker to be running locally.
- Each infrastructure test suite spins up its own ephemeral SQL Server container.
- Tests are isolated and leave no state behind.
- Test coverage thresholds are configured in the solution (see `Directory.Build.props`).

### Continuous Integration

- CI is configured through GitHub Actions (`.github/workflows/ci.yml`).
- On every push or pull request:
  - The solution is built against .NET 8.
  - Unit tests run on all jobs.
  - Integration tests run inside GitHub-hosted runners with Docker support.
- The workflow fails if:
  - Any test fails.
  - Test coverage drops below the configured threshold.
 
---

### Local Development

- Requires **.NET 8 SDK**, **Node.js 20+**, and **Docker Desktop** (for integration tests).
- Backend and frontend can be run independently or together via `docker-compose` (see `infra/` folder).

---

### License

This project is licensed under the MIT License. See the [LICENSE](./LICENSE) file for details.

# Tests

This folder will contain unit and integration tests for the Virtual Factory project.

## Planned Coverage

| Layer        | Test Type   | Notes                                              |
|--------------|-------------|----------------------------------------------------|
| Models       | Unit        | Validation, default values, enum correctness       |
| Repositories | Unit        | In-memory CRUD behaviour, filtering, not-found     |
| Services     | Unit        | Simulation logic, seed loader, profile resolution  |
| Endpoints    | Integration | API response shape, status codes, seed data round-trip |

## Running Tests

```powershell
dotnet test
```

Tests will be added as the repository and service layers are implemented in v0.2+.

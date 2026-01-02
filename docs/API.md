# API Reference

## REST API Conventions

- **Base Path**: `/api/v1`
- **Content-Type**: `application/json`
- **Authentication**: Bearer token (JWT) in `Authorization` header
- **Pagination**: `?page=1&pageSize=20`
- **Sorting**: `?sortBy=createdAt&sortOrder=desc`
- **Filtering**: `?filter[status]=confirmed&filter[visibility]=public`

## Core Endpoints

### Events

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/v1/events` | List events (filterable, paginated) |
| `GET` | `/api/v1/events/{id}` | Get event by ID |
| `POST` | `/api/v1/events` | Create event |
| `PUT` | `/api/v1/events/{id}` | Update event |
| `DELETE` | `/api/v1/events/{id}` | Delete event |
| `POST` | `/api/v1/events/{id}/join` | Join event |
| `DELETE` | `/api/v1/events/{id}/join` | Leave event |
| `GET` | `/api/v1/events/{id}/participants` | List participants |

### Organizations

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/v1/organizations` | List organizations |
| `GET` | `/api/v1/organizations/{id}` | Get organization |
| `POST` | `/api/v1/organizations` | Create organization |
| `PUT` | `/api/v1/organizations/{id}` | Update organization |
| `POST` | `/api/v1/organizations/{id}/verify` | Request verification |

### ActivityPub

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/.well-known/webfinger` | Actor discovery |
| `GET` | `/actors/{username}` | Get actor profile |
| `POST` | `/actors/{username}/inbox` | Receive activity |
| `GET` | `/actors/{username}/outbox` | Get outgoing activities |
| `GET` | `/actors/{username}/followers` | Get followers |
| `GET` | `/actors/{username}/following` | Get following |

## API Documentation

- **Scalar**: `https://localhost:7001/scalar/v1`
- **Swagger UI**: `https://localhost:7001/swagger`
- **OpenAPI Spec**: `https://localhost:7001/swagger/v1/swagger.json`

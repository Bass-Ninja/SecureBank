# SecureBank

SecureBank is a security-focused banking API and browser application built with .NET, PostgreSQL, Keycloak, and Docker. It models account ownership, money transfers, beneficiaries, staff access, and the authorization boundaries expected in a financial system.

The project is intentionally **secure by default**. Its purpose is to demonstrate application-security engineering and provide a realistic local target for authorization testing, traffic inspection, threat modelling, and defensive writeups.

## What it demonstrates

- OpenID Connect login with Keycloak and Authorization Code Flow with PKCE
- JWT issuer, audience, lifetime, and signing-key validation
- Customer and bank-staff role separation
- Object-level authorization on account resources
- Ownership checks on transfers and beneficiaries
- Idempotent money transfers and transactional persistence
- Request validation, consistent Problem Details responses, and rate limiting
- Security headers, health checks, non-root containers, and protected data-protection keys
- Unit and integration tests around authorization-sensitive behavior

## User experiences

### Customer banking

Customers can:

- View only their own accounts and balances
- Create transfers from accounts they own
- Save and remove their own beneficiaries
- Review incoming and outgoing account activity
- Filter, sort, and paginate transfer history

### Staff operations

Support staff receive a separate dashboard and can:

- Look up a customer by Keycloak user ID
- Review that customer's account numbers, balances, and status
- Use a read-only support workflow

Staff cannot initiate customer transfers or manage customer beneficiaries. The frontend reflects this boundary, but the API remains the authority: staff endpoints require the `support` or `admin` role at both the HTTP authorization-policy layer and the application request layer.

## Architecture

```text
Browser
  |
  | OIDC + PKCE
  v
Keycloak :8081 ---- JWT ----> SecureBank Web :3000
                                  |
                                  | Bearer token
                                  v
                            SecureBank API :8080
                                  |
                                  v
                             PostgreSQL :5432
```

The backend follows a layered architecture:

```text
src/
|-- SecureBank.Api                      HTTP endpoints and middleware
|-- SecureBank.Application              Commands, queries, validators, behaviors
|-- SecureBank.Application.Abstractions Shared contracts and authorization roles
|-- SecureBank.Domain                   Accounts, transfers, money, domain events
`-- SecureBank.Infrastructure           EF Core, Keycloak auth, persistence, health

frontend/                               Vite browser client served by Nginx
infrastructure/keycloak/                Reproducible realm configuration
tests/                                  Unit and integration tests
```

Commands and queries pass through validation, authorization, logging, performance, and transaction behaviors before reaching domain and persistence code.

## Security boundaries

| Resource or operation | Customer | Support/Admin | Enforcement |
|---|---:|---:|---|
| List own accounts | Yes | No customer context | Authenticated query scoped to token subject |
| Read account by ID | Own only | Any customer account | Resource-based authorization policy |
| Create transfer | From owned account | No | Ownership check in command handler |
| View transfer history | Own accounts only | No | Source/destination account ownership query |
| Manage beneficiaries | Own only | No | User-scoped commands and queries |
| Look up customer accounts | No | Yes | Staff policy plus application role requirement |

The browser controls are usability features, not security controls. Removing a button or hiding a route does not grant or deny access; every protected decision is repeated on the server.

## Run locally

### Requirements

- Docker Desktop with Docker Compose
- Ports `3000`, `8080`, and `8081` available

Start the complete environment:

```powershell
docker compose up --build
```

Open:

- Web application: <http://localhost:3000>
- API documentation: <http://localhost:8080/swagger>
- Keycloak administration: <http://localhost:8081>

The API automatically applies database migrations and creates repeatable development data.

### Demo identities

| Persona | Username | Password | Role |
|---|---|---|---|
| Customer | `nina` | `SecureBank123!` | `customer` |
| Support agent | `staff1` | `SecureBank123!` | `support` |

For the staff lookup demo, Nina's user ID is:

```text
0406f376-1a90-45a8-a117-09a96c051983
```

These credentials are development fixtures only. Do not reuse them or expose this Compose configuration to an untrusted network.

### Reset the environment

To recreate the database and Keycloak realm from their checked-in definitions:

```powershell
docker compose down --volumes
docker compose up --build
```

Keycloak startup imports only `infrastructure/keycloak/securebank-realm.json`. Mounting that individual file prevents unrelated JSON documents from being interpreted as full realm exports.

## API surface

| Method | Route | Purpose |
|---|---|---|
| `GET` | `/api/auth/me` | Current sanitized identity and application roles |
| `GET` | `/api/accounts` | Current customer's accounts |
| `GET` | `/api/accounts/{id}` | Account protected by object-level authorization |
| `GET` | `/api/transfers` | Current customer's transfer history |
| `POST` | `/api/transfers` | Idempotent transfer from an owned account |
| `GET` | `/api/beneficiaries` | Current customer's beneficiaries |
| `POST` | `/api/beneficiaries` | Add a beneficiary |
| `DELETE` | `/api/beneficiaries/{id}` | Remove an owned beneficiary |
| `GET` | `/api/staff/accounts/{userId}` | Staff-only customer account lookup |
| `GET` | `/health/live` | Process liveness |
| `GET` | `/health/ready` | Database-backed readiness |

## Testing

Run the backend test suite:

```powershell
dotnet test SecureBank.slnx
```

Build the frontend:

```powershell
cd frontend
npm install
npm run build
```

The current suite covers domain behavior, validation, application authorization, transaction handling, account isolation, and transfer-history visibility for both senders and recipients.

## Security testing and future labs

SecureBank is suitable for testing with a browser proxy such as Burp Suite even though the UI does not expose arbitrary resource IDs. A tester can authenticate normally, intercept an API request, and modify identifiers or claims-related context to verify that the server rejects unauthorized access.

Planned writeups will use a repeatable format:

1. Define the authorization claim and expected trust boundary.
2. Capture a legitimate request from the customer or staff workflow.
3. Change one object identifier, role context, or request parameter.
4. Record the response and relevant application log evidence.
5. Trace the defensive control to its implementation and automated test.
6. Document impact if the control were absent and propose regression coverage.

Candidate labs include:

- BOLA/IDOR attempts against account and beneficiary identifiers
- Cross-account transfer attempts using a foreign source account ID
- Horizontal versus vertical privilege-boundary tests
- JWT audience, issuer, expiry, and role-tampering validation
- Transfer replay and idempotency-key behavior
- Rate-limit verification and error-response analysis
- Dependency, secret, container, and static-analysis scanning
- Detection engineering based on rejected authorization attempts

All offensive testing should be performed only against the local lab environment or another system where explicit authorization has been granted.

## Project status

The core application and its customer/staff workflows are complete enough to serve as the stable target for the next phase: structured security labs, evidence capture, remediation comparisons, and portfolio writeups.

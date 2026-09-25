# SecureBank

SecureBank is a security-focused banking API and browser application built with .NET, PostgreSQL, Keycloak, and Docker. It models account ownership, money transfers, beneficiaries, staff access, and the authorization boundaries expected in a financial system.

The project is intentionally **secure by default**. Its purpose is to demonstrate application-security engineering and provide a realistic local target for authorization testing, traffic inspection, threat modelling, and defensive writeups.

## What it demonstrates

- OpenID Connect login with Keycloak and Authorization Code Flow with PKCE
- JWT issuer, audience, lifetime, and signing-key validation
- Separation of the public OIDC issuer from internal metadata and JWKS retrieval
- HTTPS for browser-facing traffic using local development certificates
- Nginx TLS termination and reverse proxying for the web application, API, and Keycloak
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
                         Docker network
                    ┌──────────────────────────┐
                    │                          │
Browser             │                          │
   │                │                          │
   │ HTTPS          │                          │
   ▼                │                          │
Nginx :3443         │                          │
   │                │                          │
   ├── /api/* ──────┼──> SecureBank API :8080 │
   │                │          │               │
   │                │          ├──> PostgreSQL │
   │                │          │      :5432    │
   │                │          │               │
   │                │          └──> Keycloak   │
   │                │               :8080      │
   │                │               metadata   │
   │                │               + JWKS     │
   │                │                          │
   └── /auth/* ─────┼──> Keycloak :8080       │
                    │                          │
                    └──────────────────────────┘
```

Nginx acts as the browser-facing reverse proxy and TLS termination point. The browser accesses the application at `https://localhost:3443`, while API and identity-provider communication inside the Docker network can use container-local HTTP.

Browser requests are routed through Nginx:

- `/api/*` → SecureBank API
- `/auth/*` → Keycloak
- all other application routes → SecureBank frontend

Keycloak issues tokens using the public issuer:

```text
https://localhost:3443/auth/realms/securebank
```

The API validates that public issuer while retrieving OpenID Connect metadata and signing keys from Keycloak through its internal Docker address.

This keeps the externally visible OIDC identity separate from container-to-container discovery without weakening JWT validation.

The backend follows a layered architecture:

```text
src/
|-- SecureBank.Api                     HTTP endpoints and middleware
|-- SecureBank.Application             Commands, queries, validators, behaviors
|-- SecureBank.Application.Abstractions Shared contracts and authorization roles
|-- SecureBank.Domain                  Accounts, transfers, money, domain events
`-- SecureBank.Infrastructure          EF Core, Keycloak auth, persistence, health

frontend/                               Vite browser client served by Nginx
infrastructure/keycloak/                Reproducible realm configuration
scripts/                                Local development setup scripts
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

### OIDC validation

The browser-facing Keycloak issuer is:

```text
https://localhost:3443/auth/realms/securebank
```

Inside Docker, the API cannot use that `localhost` address to retrieve provider metadata or signing keys because `localhost` would refer to the API container itself rather than Keycloak.

SecureBank therefore separates token identity from backchannel discovery:

- Tokens must contain the expected public issuer
- The API validates the expected audience
- Token lifetime validation remains enabled
- Signing-key validation remains enabled
- OpenID Connect metadata and signing keys are retrieved from Keycloak through its internal Docker address

This preserves the public issuer used by the browser-facing OIDC flow while allowing the API to securely retrieve Keycloak's signing keys inside the Docker network.

## Run locally

### Requirements

- Docker Desktop with Docker Compose
- .NET 10 SDK
- PowerShell
- Ports `3000`, `3443`, `8080`, `8081`, `8443`, and `15432` available

### 1. Create local development certificates

Certificates and certificate passwords are intentionally not stored in the repository.

From the repository root, run:

```powershell
.\scripts\setup-dev-cert.ps1
```

The setup script:

- Ensures a trusted ASP.NET development certificate is available
- Exports it for use by Kestrel
- Creates the certificate and private key used by Nginx
- Generates the local `.env` containing the certificate password

Generated certificate material is stored under:

```text
.certs/
```

The resulting local files include:

```text
.certs/
|-- securebank.pfx
|-- securebank.crt
`-- securebank.key

.env
```

Both `.certs/` and `.env` are excluded from Git.

The repository contains `.env.example` to document the required environment variable without storing its value:

```dotenv
SECUREBANK_CERT_PASSWORD=
```

Do not commit generated certificates, private keys, or the populated `.env` file.

### 2. Start the environment

Once the development certificates have been created, start the complete environment:

```powershell
docker compose up --build
```

Open:

- Web application: `https://localhost:3443`
- API documentation: `https://localhost:8443/swagger`
- Keycloak Admin Console: `https://localhost:3443/auth/admin/master/console/`

The API automatically applies database migrations and creates repeatable development data.

Keycloak configuration is imported automatically from the checked-in realm definition. A fresh environment does not require manually creating realms, clients, roles, groups, or demo users through the Keycloak administration console.

### Demo identities

| Persona | Username | Password | Role |
|---|---|---|---|
| Customer | `nina` | `SecureBank123!` | `customer` |
| Support agent | `staff1` | `SecureBank123!` | `support` |

For the staff lookup demo, Nina's user ID is:

```text
0406f376-1a90-45a8-a117-09a96c051983
```

Keycloak development administrator:

```text
Username: admin
Password: admin_dev_password
```

These credentials are development fixtures only. Do not reuse them or expose this Compose configuration to an untrusted network.

### Reset the environment

To recreate the database and Keycloak realm from their checked-in definitions:

```powershell
docker compose down --volumes
docker compose up --build
```

This removes the persistent PostgreSQL and Keycloak volumes. It does not remove the locally generated development certificates or `.env`.

Keycloak imports `infrastructure/keycloak/securebank-realm.json` when a new realm database is created.

The checked-in realm definition contains the clients, roles, groups, users, redirect URIs, and web origins required by the local environment.

Only the realm export itself is mounted into Keycloak's import directory, preventing unrelated JSON documents from being interpreted as realm definitions.

## HTTPS and reverse proxying

The local environment exposes two HTTPS entry points for different purposes.

The primary browser-facing application is:

```text
https://localhost:3443
```

Nginx terminates TLS on this endpoint and routes requests to the appropriate internal service.

Application API requests use:

```text
https://localhost:3443/api/*
```

and are forwarded internally to:

```text
http://api:8080
```

Keycloak requests use:

```text
https://localhost:3443/auth/*
```

and are forwarded internally to the Keycloak container.

Security headers applied by Nginx to the SecureBank application are scoped to the application routes. Keycloak responses under `/auth/*` retain Keycloak's own security-header policy because its authentication and administration flows use legitimate iframe-based browser mechanisms.

The API also exposes a direct HTTPS development endpoint:

```text
https://localhost:8443
```

This is useful for Swagger, direct API testing, and security labs such as comparing plaintext HTTP traffic with TLS-protected traffic.

The certificates used by these endpoints are generated locally and are not part of the repository.

## API surface

| Method | Route | Purpose |
|---|---|---|
| `GET` | `/api/auth/me` | Current sanitized identity and application roles |
| `GET` | `/api/accounts` | Current customer's accounts |
| `GET` | `/api/accounts/{id}` | Account protected by object-level authorization |
| `GET` | `/api/transfers` | Current customer's transfer history |
| `POST` | `/api/transfers` | Idempotent transfer from an owned account to an account number |
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

The application can also be used as a target from an isolated security-testing environment, allowing the normal application stack to remain separate from tooling used for traffic analysis, enumeration, and controlled attack simulations.

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
- HTTP versus HTTPS traffic analysis
- TLS handshake and encrypted application-traffic inspection
- Rate-limit verification and error-response analysis
- Dependency, secret, container, and static-analysis scanning
- Detection engineering based on rejected authorization attempts

All offensive testing should be performed only against the local lab environment or another system where explicit authorization has been granted.

## Project status

The core application and its customer/staff workflows are complete enough to serve as the stable target for the next phase: structured security labs, evidence capture, remediation comparisons, and portfolio writeups.

The current environment includes reproducible Keycloak configuration, containerized PostgreSQL, HTTPS-enabled browser and API access, OIDC/JWT validation, and a Docker-based application stack that can be used as the target for future security-engineering labs.
# SecureBank

SecureBank is a security-focused banking API and browser application built with .NET, PostgreSQL, Keycloak, Nginx, and Docker. It models account ownership, money transfers, beneficiaries, staff access, and the authorization boundaries expected in a financial system.

The project is intentionally **secure by default**. Its purpose is to demonstrate application-security engineering and provide a realistic target for authorization testing, traffic inspection, threat modelling, network-security experiments, and defensive writeups.

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
- Restricted Docker service exposure
- Internal-only database and API communication where direct host exposure is unnecessary
- Unit and integration tests around authorization-sensitive behavior

## User Experiences

### Customer Banking

Customers can:

- View only their own accounts and balances
- Create transfers from accounts they own
- Save and remove their own beneficiaries
- Review incoming and outgoing account activity
- Filter, sort, and paginate transfer history

### Staff Operations

Support staff receive a separate dashboard and can:

- Look up a customer by Keycloak user ID
- Review that customer's account numbers, balances, and status
- Use a read-only support workflow

Staff cannot initiate customer transfers or manage customer beneficiaries.

The frontend reflects this boundary, but the API remains the authority: staff endpoints require the `support` or `admin` role at both the HTTP authorization-policy layer and the application request layer.

## Architecture

```text
                          Docker network

                    ┌─────────────────────────────┐
                    │                             │
External client     │                             │
      │             │                             │
      │ HTTPS       │                             │
      ▼             │                             │
 Nginx :3443 ───────┼─────────────────────────┐   │
      │             │                         │   │
      ├── /api/* ───┼──> SecureBank API :8080│   │
      │             │          │              │   │
      │             │          ├──> PostgreSQL│   │
      │             │          │      :5432   │   │
      │             │          │              │   │
      │             │          └──> Keycloak  │   │
      │             │               :8080     │   │
      │             │               metadata  │   │
      │             │               + JWKS    │   │
      │             │                         │   │
      └── /auth/* ──┼────────────> Keycloak   │   │
                    │                 :8080    │   │
                    │                         │   │
                    └─────────────────────────────┘
```

Nginx acts as the primary browser-facing reverse proxy and TLS termination point.

The browser accesses the application at:

```text
https://localhost:3443
```

During future isolated security labs, the same entry point can be reached through the target machine's lab-network address or dedicated hostname.

Browser requests are routed through Nginx:

- `/api/*` → SecureBank API
- `/auth/*` → Keycloak
- all other application routes → SecureBank frontend

The API and PostgreSQL do not require directly published HTTP/database ports for normal application operation.

Internal services communicate using Docker networking:

```text
web → api:8080
api → postgres:5432
api → keycloak:8080
```

This keeps backend communication available while reducing unnecessary host exposure.

Keycloak issues tokens using the public issuer:

```text
https://localhost:3443/auth/realms/securebank
```

The API validates that public issuer while retrieving OpenID Connect metadata and signing keys from Keycloak through its internal Docker address.

This keeps the externally visible OIDC identity separate from container-to-container discovery without weakening JWT validation.

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
scripts/                                Local development setup scripts
tests/                                  Unit and integration tests
```

Commands and queries pass through validation, authorization, logging, performance, and transaction behaviors before reaching domain and persistence code.

## Network Exposure Model

SecureBank deliberately distinguishes between services that require host access and services that should remain internal.

| Service | Container Port | Host Exposure | Purpose |
| --- | ---: | --- | --- |
| Web HTTPS | `443` | `0.0.0.0:3443` / `[::]:3443` | Primary external application entry point |
| Web HTTP | `80` | `127.0.0.1:3000` | Local HTTP testing and redirect behavior |
| API HTTPS | `8443` | `127.0.0.1:8443` | Local Swagger and direct API development |
| API HTTP | `8080` | Not published | Internal Nginx/API and health-check communication |
| Keycloak HTTP | `8080` | `127.0.0.1:8081` | Local development/admin access |
| PostgreSQL | `5432` | Not published | Internal API/database communication |

The externally reachable HTTPS frontend is intentional because it represents the application's normal public attack surface.

By contrast, the API's internal HTTP listener and PostgreSQL database do not require direct host exposure.

This distinction is useful both for hardening and for security testing:

```text
External client / Kali VM
          │
          │ HTTPS
          ▼
      Nginx :3443
          │
     Docker network
       ┌──┴───────┐
       ▼          ▼
      API      Keycloak
       │
       ▼
   PostgreSQL
```

A future attacker VM should therefore interact with SecureBank through the externally intended application surface rather than through unnecessarily exposed backend service ports.

## Security Boundaries

| Resource or operation | Customer | Support/Admin | Enforcement |
| --- | ---: | ---: | --- |
| List own accounts | Yes | No customer context | Authenticated query scoped to token subject |
| Read account by ID | Own only | Any customer account | Resource-based authorization policy |
| Create transfer | From owned account | No | Ownership check in command handler |
| View transfer history | Own accounts only | No | Source/destination account ownership query |
| Manage beneficiaries | Own only | No | User-scoped commands and queries |
| Look up customer accounts | No | Yes | Staff policy plus application role requirement |

The browser controls are usability features, not security controls.

Removing a button or hiding a route does not grant or deny access; every protected decision is repeated on the server.

### OIDC Validation

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

## Run Locally

### Requirements

- Docker Desktop with Docker Compose
- .NET 10 SDK
- PowerShell
- Host ports `3000`, `3443`, `8081`, and `8443` available

Ports `8080` and `5432` are used internally by Docker services and are not published directly to the host.

### 1. Create Local Development Certificates

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

### 2. Start the Environment

Once the development certificates have been created, start the complete environment:

```powershell
docker compose up --build
```

Open:

- Web application: `https://localhost:3443`
- API documentation: `https://localhost:8443/swagger`
- Keycloak Admin Console: `https://localhost:3443/auth/admin/master/console/`

The API automatically applies database migrations and creates repeatable development data.

Keycloak configuration is imported automatically from the checked-in realm definition.

A fresh environment does not require manually creating realms, clients, roles, groups, or demo users through the Keycloak administration console.

### Demo Identities

| Persona | Username | Password | Role |
| --- | --- | --- | --- |
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

These credentials are development fixtures only.

Do not reuse them in another environment or expose this development configuration directly to an untrusted network.

### Reset the Environment

To recreate the database and Keycloak realm from their checked-in definitions:

```powershell
docker compose down --volumes
docker compose up --build
```

This removes the persistent PostgreSQL and Keycloak volumes.

It does not remove the locally generated development certificates or `.env`.

Keycloak imports:

```text
infrastructure/keycloak/securebank-realm.json
```

when a new realm database is created.

The checked-in realm definition contains the clients, roles, groups, users, redirect URIs, and web origins required by the local environment.

Only the realm export itself is mounted into Keycloak's import directory, preventing unrelated JSON documents from being interpreted as realm definitions.

## HTTPS and Reverse Proxying

The local environment exposes two HTTPS entry points for different purposes.

### Primary Application Entry Point

The primary browser-facing application is:

```text
https://localhost:3443
```

Nginx terminates TLS on this endpoint and routes requests to the appropriate internal service.

Port `3443` is deliberately published beyond loopback because it represents the application's intended external entry point and can later be reached from an isolated security-testing VM.

Requests to the local HTTP frontend:

```text
http://localhost:3000
```

are restricted to loopback and redirected to the HTTPS application.

Application API requests use:

```text
https://localhost:3443/api/*
```

and are forwarded internally to:

```text
http://api:8080
```

The API's HTTP port `8080` is not published to the host.

Keycloak requests use:

```text
https://localhost:3443/auth/*
```

and are forwarded internally to the Keycloak container.

Security headers applied by Nginx to the SecureBank application are scoped to application routes.

Keycloak responses under `/auth/*` retain Keycloak's own security-header policy because its authentication and administration flows use legitimate iframe-based browser mechanisms.

### Direct API Development Endpoint

The API also exposes a direct HTTPS development endpoint:

```text
https://localhost:8443
```

This is useful for:

- Swagger
- direct API testing
- debugging
- security labs requiring direct API interaction

The endpoint is explicitly bound to `127.0.0.1` and is therefore not intended as part of the remotely accessible application surface.

Remote lab clients should normally use:

```text
https://<target-host>:3443/api/*
```

through Nginx.

The generated development certificate is valid for local hostnames and loopback addresses.

Testing through a VM IP or dedicated lab hostname will require a certificate containing that hostname or address in its Subject Alternative Name configuration.

### PostgreSQL

PostgreSQL is intentionally not published to the host.

The API connects directly using:

```text
postgres:5432
```

over the Docker network.

This was verified by removing the previous localhost database mapping and confirming that:

- direct host access to PostgreSQL failed
- SecureBank authentication continued to work
- database-backed application data continued loading normally

This reduces unnecessary host exposure without affecting application functionality.

### HSTS

HSTS is intentionally omitted from the localhost development profile because HSTS applies to a hostname across ports and would interfere with explicit local HTTP experiments used for protocol-comparison labs.

A deployment using a dedicated production hostname should enable HSTS after HTTPS is fully established.

The certificates used by these endpoints are generated locally and are not part of the repository.

## API Surface

| Method | Route | Purpose |
| --- | --- | --- |
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

## Security Testing and Future Labs

SecureBank is suitable for testing with a browser proxy such as Burp Suite even though the UI does not expose arbitrary resource IDs.

A tester can authenticate normally, intercept an API request, and modify identifiers or claims-related context to verify that the server rejects unauthorized access.

The application can also be deployed as a target in an isolated security-testing environment.

A planned setup uses a separate Kali Linux VM to interact with SecureBank over an isolated virtual network:

```text
Kali Linux VM
      │
      │ controlled security testing
      ▼
Target VM
      │
      ▼
SecureBank :3443
      │
      ▼
Docker internal services
```

This allows:

- service enumeration
- attack-surface verification
- HTTP/API testing
- authentication and authorization testing
- traffic analysis
- controlled attack simulation

without exposing unnecessary backend services directly.

Planned writeups will use a repeatable format:

1. Define the authorization claim or security boundary.
2. Capture the expected legitimate behavior.
3. Modify one identifier, role context, request parameter, or network condition.
4. Record the response and relevant evidence.
5. Trace the defensive control to its implementation.
6. Verify whether the control behaves as expected.
7. Document the potential impact if the control were absent.

Candidate labs include:

- Docker network exposure and service reachability
- Nmap service enumeration
- Firewall and network segmentation
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

## Related Security Portfolio

The security experiments built around SecureBank are documented separately in the cybersecurity portfolio repository.

SecureBank remains the application source repository, while the portfolio contains the security methodology, evidence, observations, findings, and writeups produced during testing.

This separation keeps the application implementation independent from the security-lab documentation while allowing both projects to reference one another.

## Project Status

The core application and its customer/staff workflows are complete enough to serve as the stable target for the next phase: structured security labs, evidence capture, remediation comparisons, and portfolio writeups.

The current environment includes:

- reproducible Keycloak configuration
- containerized PostgreSQL
- HTTPS-enabled browser and API access
- OIDC/JWT validation
- role and resource-based authorization
- internal Docker service networking
- restricted host port exposure
- a public-facing Nginx entry point suitable for future isolated VM testing
- automated backend security and authorization tests

SecureBank is now intended to remain a stable, secure-by-default target while the surrounding security portfolio evaluates its network exposure, application controls, identity boundaries, monitoring, and defensive behavior.
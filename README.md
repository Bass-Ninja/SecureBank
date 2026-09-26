# SecureBank

SecureBank is a security-focused banking API and browser application built with .NET, PostgreSQL, Keycloak, Nginx, and Docker. It models account ownership, money transfers, beneficiaries, staff access, and the authorization boundaries expected in a financial system.

The project is intentionally **secure by default**. Its purpose is to demonstrate application-security engineering and provide a realistic target for authorization testing, traffic inspection, threat modelling, network-security experiments, and defensive writeups.

## What It Demonstrates

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
- Separate local-development and security-lab deployment profiles
- Host firewall enforcement for management-plane access
- Firewall logging for blocked security-lab traffic
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

For normal local development, the browser accesses the application at:

```text
https://localhost:3443
```

The same application can also be deployed into an isolated security lab where it is accessed as:

```text
https://securebank.lab:3443
```

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

For local development, Keycloak issues tokens using the public issuer:

```text
https://localhost:3443/auth/realms/securebank
```

In the security-lab deployment, the public issuer becomes:

```text
https://securebank.lab:3443/auth/realms/securebank
```

The API validates the public issuer while retrieving OpenID Connect metadata and signing keys from Keycloak through its internal Docker address.

This keeps externally visible OIDC identity separate from container-to-container discovery without weakening JWT validation.

The backend follows a layered architecture:

```text
src/
|-- SecureBank.Api                      HTTP endpoints and middleware
|-- SecureBank.Application              Commands, queries, validators, behaviors
|-- SecureBank.Application.Abstractions Shared contracts and authorization roles
|-- SecureBank.Domain                   Accounts, transfers, money, domain events
`-- SecureBank.Infrastructure           EF Core, Keycloak auth, persistence, health

frontend/                               Vite browser client served by Nginx
infrastructure/keycloak/                Reproducible Keycloak realm configuration
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

The intended remote path is:

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

### Externally Observed Lab Attack Surface

External enumeration from the Kali VM was initially able to discover:

| Port | Service | Initial State | Purpose |
| ---: | --- | --- | --- |
| `22` | OpenSSH | Open | Ubuntu target administration |
| `3443` | SecureBank HTTPS / Nginx | Open | Public application surface |

After host firewall rules were introduced on the Ubuntu target, the externally observed state became:

| Port | Service | Current State | Purpose |
| ---: | --- | --- | --- |
| `22` | OpenSSH | Filtered from Kali | Ubuntu target administration |
| `3443` | SecureBank HTTPS / Nginx | Open | Public application surface |

The SSH service remains active on Ubuntu, but connections arriving from the attacker-facing lab interface are blocked by UFW.

The following backend ports are intentionally not remotely exposed:

| Port | Service |
| ---: | --- |
| `5432` | PostgreSQL |
| `8080` | API HTTP |
| `8081` | Direct Keycloak HTTP |
| `8443` | Direct API HTTPS |

These assumptions are verified externally from the Kali VM rather than inferred only from Docker configuration.

### Public Application vs Management Plane

The security-lab configuration distinguishes between the intended application surface and the management plane.

```text
Kali attacker
     │
     ├── TCP/3443 → SecureBank HTTPS → allowed
     │
     └── TCP/22   → SSH              → filtered
```

SecureBank remains remotely accessible while SSH management access is blocked from the attacker network.

This reduces unnecessary management-plane exposure without stopping the SSH daemon itself.

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

The browser-facing Keycloak issuer depends on the deployment profile.

Local development:

```text
https://localhost:3443/auth/realms/securebank
```

Security lab:

```text
https://securebank.lab:3443/auth/realms/securebank
```

Inside Docker, the API cannot use those public addresses to retrieve provider metadata or signing keys because `localhost` refers to the API container itself and the external lab hostname represents the public route rather than the internal Keycloak service.

SecureBank therefore separates token identity from backchannel discovery:

- Tokens must contain the expected public issuer
- The API validates the expected audience
- Token lifetime validation remains enabled
- Signing-key validation remains enabled
- OpenID Connect metadata and signing keys are retrieved from Keycloak through its internal Docker address

This preserves the public issuer used by the browser-facing OIDC flow while allowing the API to securely retrieve Keycloak's signing keys inside Docker.

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

### 2. Start the Local Environment

Once the development certificates have been created, start the complete environment:

```powershell
docker compose up --build
```

Open:

- Web application: `https://localhost:3443`
- API documentation: `https://localhost:8443/swagger`
- Keycloak Admin Console: `https://localhost:3443/auth/admin/master/console/`

The API automatically applies database migrations and creates repeatable development data.

Keycloak configuration is imported automatically from the checked-in local realm definition.

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

### Reset the Local Environment

To recreate the database and Keycloak realm from their checked-in definitions:

```powershell
docker compose down --volumes
docker compose up --build
```

This removes the persistent PostgreSQL and Keycloak volumes.

It does not remove the locally generated development certificates or `.env`.

The default Keycloak realm definition is:

```text
infrastructure/keycloak/securebank-realm.json
```

The checked-in realm definition contains the clients, roles, groups, users, redirect URIs, and web origins required by the local environment.

## Security Lab Deployment

SecureBank can also be deployed as a target inside an isolated VirtualBox security lab.

### Lab Topology

```text
                    Internet
                       │
                VirtualBox NAT
                  │         │
                  │         │
               Kali       Ubuntu
                  │         │
                  └────┬────┘
                       │
                securebank-lab
                192.168.56.0/24
                       │
            ┌──────────┴──────────┐
            │                     │
      Kali attacker        SecureBank target
      192.168.56.10        192.168.56.20
                                  │
                                  ▼
                               Docker
```

Current lab addressing:

```text
Kali Linux:       192.168.56.10
Ubuntu target:    192.168.56.20
Lab hostname:     securebank.lab
```

The Kali VM resolves:

```text
securebank.lab → 192.168.56.20
```

The public application entry point is:

```text
https://securebank.lab:3443
```

### Lab Deployment Files

The security lab uses:

```text
docker-compose.yml
docker-compose.lab.yml
```

The base Compose file defines the common application stack.

The lab override changes only configuration that differs in the VM environment, such as:

- public OIDC issuer
- Keycloak public hostname
- lab-specific Keycloak realm import

The lab-specific Keycloak configuration is stored in:

```text
infrastructure/keycloak/securebank-realm.lab.json
```

This allows local development to continue using `localhost` while the VM deployment uses `securebank.lab`.

### Lab Certificates

The Ubuntu target uses locally generated certificates containing the security-lab hostname and target address in the Subject Alternative Name configuration.

The certificate is intentionally self-signed for the isolated lab environment.

It is not intended to represent a production PKI configuration.

### Start the Lab Environment

From the Ubuntu target:

```bash
docker compose \
  -f docker-compose.yml \
  -f docker-compose.lab.yml \
  up -d --build
```

Check status:

```bash
docker compose \
  -f docker-compose.yml \
  -f docker-compose.lab.yml \
  ps
```

Reset the disposable lab database and Keycloak state when a clean realm import is required:

```bash
docker compose \
  -f docker-compose.yml \
  -f docker-compose.lab.yml \
  down --volumes
```

Then start the environment again.

### Host Firewall and Service Segmentation

The Ubuntu target uses UFW to distinguish between the public application surface and management access.

The lab-facing Ubuntu interface is:

```text
enp0s8
192.168.56.20/24
```

The firewall uses a default-deny incoming policy:

```bash
sudo ufw default deny incoming
sudo ufw default allow outgoing
```

SecureBank HTTPS is explicitly allowed through the lab interface:

```bash
sudo ufw allow in on enp0s8 to any port 3443 proto tcp
```

SSH is blocked from the attacker-facing interface and matching attempts are logged:

```bash
sudo ufw deny in on enp0s8 log proto tcp to any port 22
```

After enabling UFW, external verification from Kali showed:

```text
22/tcp   filtered
3443/tcp open
```

SecureBank continued to return:

```text
HTTP/1.1 200 OK
```

while SSH connections from Kali timed out.

The SSH daemon itself remained:

```text
active (running)
```

and continued listening on port `22`.

This confirms that the security control restricts network access rather than disabling the management service.

### Firewall Logging

Blocked SSH attempts from Kali generate firewall events containing fields such as:

```text
SRC=192.168.56.10
DST=192.168.56.20
DPT=22
PROTO=TCP
```

This provides both:

```text
prevention
+
visibility
```

The same telemetry can later be forwarded into a SIEM such as Wazuh for detection engineering and alerting.

### Host Firewall vs Full Network Segmentation

Kali and Ubuntu remain on the same subnet:

```text
192.168.56.0/24
```

The current control therefore represents:

```text
host firewall enforcement
+
service segmentation
```

rather than full VLAN or subnet-based network segmentation.

A future expansion could introduce separate management and attacker networks with routing and firewall policy between them.

## HTTPS and Reverse Proxying

### Primary Application Entry Point

For local development:

```text
https://localhost:3443
```

For the isolated security lab:

```text
https://securebank.lab:3443
```

Nginx terminates TLS on this endpoint and routes requests to the appropriate internal service.

Port `3443` is deliberately published beyond loopback because it represents the application's intended externally reachable entry point.

Local frontend HTTP:

```text
http://localhost:3000
```

is restricted to loopback and redirected to HTTPS.

Application API requests use:

```text
https://<public-host>:3443/api/*
```

and are forwarded internally to:

```text
http://api:8080
```

The API's HTTP port `8080` is not published to the host.

Keycloak requests use:

```text
https://<public-host>:3443/auth/*
```

and are forwarded internally to the Keycloak container.

Security headers applied by Nginx to SecureBank application routes include:

- Content Security Policy
- `X-Content-Type-Options`
- `X-Frame-Options`
- Referrer Policy
- Permissions Policy

Keycloak responses under `/auth/*` retain Keycloak's own security-header policy because its authentication and administration flows use legitimate iframe-based browser mechanisms.

### Nginx Server Version Disclosure

External service enumeration initially revealed:

```text
Server: nginx/1.29.8
```

The Nginx configuration was hardened using:

```nginx
server_tokens off;
```

After rebuilding the web container, the response was verified again:

```text
Server: nginx
```

This does not conceal the use of Nginx, but it removes unnecessary exact version information from normal HTTP responses.

### Direct API Development Endpoint

The API also exposes a direct HTTPS development endpoint:

```text
https://localhost:8443
```

This is useful for:

- Swagger
- direct API testing
- debugging
- local security experiments

The endpoint is explicitly bound to `127.0.0.1` and is therefore not intended as part of the remotely accessible application surface.

Remote lab clients normally use:

```text
https://securebank.lab:3443/api/*
```

through Nginx.

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

HSTS is intentionally omitted from the localhost development profile because HSTS applies to a hostname across ports and would interfere with explicit HTTP experiments used for protocol-comparison labs.

A dedicated production hostname should enable HSTS after HTTPS is fully established.

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

## Security Testing

SecureBank is suitable for testing with a browser proxy such as Burp Suite even though the UI does not expose arbitrary resource IDs.

A tester can authenticate normally, intercept an API request, and modify identifiers or claims-related context to verify that the server rejects unauthorized access.

The application is also deployed into an isolated security-testing environment using a dedicated Kali Linux attacker VM and Ubuntu target VM.

```text
Kali Linux VM
192.168.56.10
      │
      │ controlled security testing
      ▼
Ubuntu target VM
192.168.56.20
      │
      ├── SSH :22 ───── filtered from Kali
      │
      ▼
SecureBank :3443
      │
      ▼
Docker internal services
```

The environment supports:

- service enumeration
- attack-surface verification
- HTTP and API testing
- TLS inspection
- authentication and authorization testing
- traffic analysis
- firewall and service-access testing
- blocked-traffic logging
- controlled attack simulation
- remediation verification

Security writeups generally follow a repeatable workflow:

1. Define the security claim or expected trust boundary.
2. Capture expected legitimate behavior.
3. Test the boundary from an external or untrusted perspective.
4. Record application, network, or log evidence.
5. Identify the defensive control responsible for the result.
6. Remediate unnecessary exposure or weakness where appropriate.
7. Repeat the original test.
8. Document the final result and lessons learned.

### Completed Security Labs

- HTTP vs HTTPS traffic analysis
- TLS handshake and encrypted application-traffic inspection
- Docker network exposure and service reachability
- PostgreSQL host-exposure reduction
- Docker-internal API communication verification
- Nmap default and full TCP port discovery
- Service and version fingerprinting
- HTTP and TLS metadata enumeration
- Nginx exact-version disclosure hardening and verification
- Host firewall and service segmentation
- SSH management-plane filtering
- UFW blocked-traffic logging
- Firewall remediation verification from Kali

### Planned Security Labs

- BOLA/IDOR attempts against account and beneficiary identifiers
- Cross-account transfer attempts using a foreign source account ID
- Horizontal versus vertical privilege-boundary tests
- JWT audience, issuer, expiry, and role-tampering validation
- Transfer replay and idempotency-key behavior
- Rate-limit verification and error-response analysis
- Burp Suite API testing
- Dependency scanning
- Secret scanning
- Container scanning
- Static analysis
- Detection engineering based on rejected authorization attempts
- Wazuh monitoring and incident investigation
- Full subnet/VLAN-based network segmentation

All offensive testing should be performed only against the isolated lab environment or another system where explicit authorization has been granted.

## Related Security Portfolio

The security experiments built around SecureBank are documented separately in the cybersecurity portfolio repository.

SecureBank remains the application source repository, while the portfolio contains the security methodology, evidence, observations, findings, remediation steps, and writeups produced during testing.

This separation keeps the application implementation independent from the security-lab documentation while allowing both projects to reference one another.

## Project Status

The core application and its customer/staff workflows are complete enough to serve as a stable target for structured security testing.

The current environment includes:

- reproducible Keycloak configuration
- separate localhost and VM-lab deployment profiles
- containerized PostgreSQL
- HTTPS-enabled browser and API access
- OIDC/JWT validation
- role and resource-based authorization
- internal Docker service networking
- restricted host port exposure
- Kali Linux attacker VM
- Ubuntu SecureBank target VM
- isolated VirtualBox security network
- externally testable SecureBank HTTPS surface
- Nginx reverse proxying and TLS termination
- reduced server-version disclosure
- UFW host firewall enforcement
- SSH management-plane filtering
- firewall logging for blocked lab traffic
- externally verified service segmentation
- automated backend security and authorization tests

SecureBank now serves as the stable application target while the surrounding cybersecurity portfolio evaluates its network exposure, application controls, identity boundaries, monitoring, and defensive behavior.
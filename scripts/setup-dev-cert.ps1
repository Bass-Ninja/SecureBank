$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$certDir = Join-Path $root ".certs"

$pfxPath = Join-Path $certDir "securebank.pfx"
$crtPath = Join-Path $certDir "securebank.crt"
$keyPath = Join-Path $certDir "securebank.key"
$envPath = Join-Path $root ".env"

Write-Host "Setting up SecureBank development certificates..."

# Check prerequisites
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw ".NET SDK is required."
}

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    throw "Docker is required."
}

# Create certificate directory
New-Item -ItemType Directory -Force -Path $certDir | Out-Null

# Generate a random, .env-safe certificate password
$password = [Guid]::NewGuid().ToString("N")

# Create and trust ASP.NET development certificate
Write-Host "Creating ASP.NET development certificate..."

dotnet dev-certs https --clean
dotnet dev-certs https --trust

dotnet dev-certs https `
    --export-path $pfxPath `
    --password $password

# Convert PFX for Nginx using OpenSSL in Docker
Write-Host "Creating Nginx certificate..."

docker run --rm `
    -v "${certDir}:/certs" `
    alpine/openssl pkcs12 `
    -in /certs/securebank.pfx `
    -clcerts `
    -nokeys `
    -out /certs/securebank.crt `
    -passin "pass:$password"

docker run --rm `
    -v "${certDir}:/certs" `
    alpine/openssl pkcs12 `
    -in /certs/securebank.pfx `
    -nocerts `
    -nodes `
    -out /certs/securebank.key `
    -passin "pass:$password"

# Create local .env
@"
SECUREBANK_CERT_PASSWORD=$password
"@ | Set-Content $envPath

Write-Host ""
Write-Host "SecureBank development certificates created successfully."
Write-Host "Certificate directory: $certDir"
Write-Host ""
Write-Host "You can now run:"
Write-Host "docker compose up --build"
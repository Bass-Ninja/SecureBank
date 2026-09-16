FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]

COPY ["src/SecureBank.Api/SecureBank.Api.csproj", "src/SecureBank.Api/"]
COPY ["src/SecureBank.Application/SecureBank.Application.csproj", "src/SecureBank.Application/"]
COPY ["src/SecureBank.Application.Abstractions/SecureBank.Application.Abstractions.csproj", "src/SecureBank.Application.Abstractions/"]
COPY ["src/SecureBank.Domain/SecureBank.Domain.csproj", "src/SecureBank.Domain/"]
COPY ["src/SecureBank.Infrastructure/SecureBank.Infrastructure.csproj", "src/SecureBank.Infrastructure/"]

RUN dotnet restore "src/SecureBank.Api/SecureBank.Api.csproj"

COPY . .

WORKDIR "/src/src/SecureBank.Api"
RUN dotnet publish "SecureBank.Api.csproj" \
    -c Release \
    -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "SecureBank.Api.dll"]
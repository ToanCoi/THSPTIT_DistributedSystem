# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY Ecom.slnx ./
COPY BE.Domain.Share/BE.Domain.Share.csproj BE.Domain.Share/
COPY BE.Domain/BE.Domain.csproj BE.Domain/
COPY BE.Application.Contracts/BE.Application.Contracts.csproj BE.Application.Contracts/
COPY BE.Domain.Mysql/BE.Domain.Mysql.csproj BE.Domain.Mysql/
COPY BE.Application/BE.Application.csproj BE.Application/
COPY BE.HostBase/BE.HostBase.csproj BE.HostBase/
COPY HandleWorker/HandleWorker.csproj HandleWorker/

# Restore dependencies
RUN dotnet restore HandleWorker/HandleWorker.csproj

# Copy source code
COPY BE.Domain.Share/ BE.Domain.Share/
COPY BE.Domain/ BE.Domain/
COPY BE.Application.Contracts/ BE.Application.Contracts/
COPY BE.Domain.Mysql/ BE.Domain.Mysql/
COPY BE.Application/ BE.Application/
COPY BE.HostBase/ BE.HostBase/
COPY HandleWorker/ HandleWorker/

# Build
RUN dotnet build HandleWorker/HandleWorker.csproj -c Release -o /app/build

# Publish
RUN dotnet publish HandleWorker/HandleWorker.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "HandleWorker.dll"]
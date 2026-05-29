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
COPY BusinessApi/BusinessApi.csproj BusinessApi/

# Restore dependencies
RUN dotnet restore BusinessApi/BusinessApi.csproj

# Copy source code
COPY BE.Domain.Share/ BE.Domain.Share/
COPY BE.Domain/ BE.Domain/
COPY BE.Application.Contracts/ BE.Application.Contracts/
COPY BE.Domain.Mysql/ BE.Domain.Mysql/
COPY BE.Application/ BE.Application/
COPY BE.HostBase/ BE.HostBase/
COPY BusinessApi/ BusinessApi/

# Build
RUN dotnet build BusinessApi/BusinessApi.csproj -c Release -o /app/build

# Publish
RUN dotnet publish BusinessApi/BusinessApi.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 5002
ENTRYPOINT ["dotnet", "BusinessApi.dll"]
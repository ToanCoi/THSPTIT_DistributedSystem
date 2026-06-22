# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY Ecom.slnx ./
COPY BE.Domain.Share/BE.Domain.Shared.csproj BE.Domain.Share/
COPY BE.Domain/BE.Domain.csproj BE.Domain/
COPY BE.Application.Contracts/BE.Application.Contracts.csproj BE.Application.Contracts/
COPY BE.Domain.Mysql/BE.Domain.Mysql.csproj BE.Domain.Mysql/
COPY BE.Application/BE.Application.csproj BE.Application/
COPY BE.HostBase/BE.HostBase.csproj BE.HostBase/
COPY Workers/Workers.Shared/Workers.Shared.csproj Workers/Workers.Shared/
COPY OrderApi/OrderApi.csproj OrderApi/

# Restore dependencies
RUN dotnet restore OrderApi/OrderApi.csproj

# Copy source code
COPY BE.Domain.Share/ BE.Domain.Share/
COPY BE.Domain/ BE.Domain/
COPY BE.Application.Contracts/ BE.Application.Contracts/
COPY BE.Domain.Mysql/ BE.Domain.Mysql/
COPY BE.Application/ BE.Application/
COPY BE.HostBase/ BE.HostBase/
COPY Workers/Workers.Shared/ Workers/Workers.Shared/
COPY OrderApi/ OrderApi/

# Build
RUN dotnet build OrderApi/OrderApi.csproj -c Release -o /app/build

# Publish
RUN dotnet publish OrderApi/OrderApi.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 5003
ENTRYPOINT ["dotnet", "OrderApi.dll"]
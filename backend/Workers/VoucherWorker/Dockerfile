# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Ecom.slnx ./
COPY BE.Domain.Share/BE.Domain.Shared.csproj BE.Domain.Share/
COPY BE.Domain/BE.Domain.csproj BE.Domain/
COPY BE.Application.Contracts/BE.Application.Contracts.csproj BE.Application.Contracts/
COPY BE.Domain.Mysql/BE.Domain.Mysql.csproj BE.Domain.Mysql/
COPY BE.Application/BE.Application.csproj BE.Application/
COPY BE.HostBase/BE.HostBase.csproj BE.HostBase/
COPY Workers/Workers.Shared/Workers.Shared.csproj Workers/Workers.Shared/
COPY Workers/VoucherWorker/VoucherWorker.csproj Workers/VoucherWorker/

RUN dotnet restore Workers/VoucherWorker/VoucherWorker.csproj

COPY BE.Domain.Share/ BE.Domain.Share/
COPY BE.Domain/ BE.Domain/
COPY BE.Application.Contracts/ BE.Application.Contracts/
COPY BE.Domain.Mysql/ BE.Domain.Mysql/
COPY BE.Application/ BE.Application/
COPY BE.HostBase/ BE.HostBase/
COPY Workers/Workers.Shared/ Workers/Workers.Shared/
COPY Workers/VoucherWorker/ Workers/VoucherWorker/

RUN dotnet build Workers/VoucherWorker/VoucherWorker.csproj -c Release -o /app/build
RUN dotnet publish Workers/VoucherWorker/VoucherWorker.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "VoucherWorker.dll"]

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props ./
COPY packages/CleanArchitecture.BuildingBlocks/src/CleanArchitecture.BuildingBlocks/CleanArchitecture.BuildingBlocks.csproj packages/CleanArchitecture.BuildingBlocks/src/CleanArchitecture.BuildingBlocks/
COPY packages/Dapr.EntityFrameworkCore.Outbox/src/Dapr.EntityFrameworkCore.Outbox/Dapr.EntityFrameworkCore.Outbox.csproj packages/Dapr.EntityFrameworkCore.Outbox/src/Dapr.EntityFrameworkCore.Outbox/
COPY packages/JobEngine.Contracts/src/JobEngine.Contracts/JobEngine.Contracts.csproj packages/JobEngine.Contracts/src/JobEngine.Contracts/
COPY services/WorkerService/src/WorkerService.Domain/WorkerService.Domain.csproj services/WorkerService/src/WorkerService.Domain/
COPY services/WorkerService/src/WorkerService.Application/WorkerService.Application.csproj services/WorkerService/src/WorkerService.Application/
COPY services/WorkerService/src/WorkerService.Infrastructure/WorkerService.Infrastructure.csproj services/WorkerService/src/WorkerService.Infrastructure/
COPY services/WorkerService/src/WorkerService.Api/WorkerService.Api.csproj services/WorkerService/src/WorkerService.Api/
RUN dotnet restore services/WorkerService/src/WorkerService.Api/WorkerService.Api.csproj

COPY packages/ packages/
COPY services/WorkerService/ services/WorkerService/
RUN dotnet publish services/WorkerService/src/WorkerService.Api/WorkerService.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:5100
EXPOSE 5100
ENTRYPOINT ["dotnet", "WorkerService.Api.dll"]

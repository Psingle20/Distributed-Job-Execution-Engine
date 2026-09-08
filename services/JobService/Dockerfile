FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props ./
COPY packages/CleanArchitecture.BuildingBlocks/src/CleanArchitecture.BuildingBlocks/CleanArchitecture.BuildingBlocks.csproj packages/CleanArchitecture.BuildingBlocks/src/CleanArchitecture.BuildingBlocks/
COPY packages/Dapr.EntityFrameworkCore.Outbox/src/Dapr.EntityFrameworkCore.Outbox/Dapr.EntityFrameworkCore.Outbox.csproj packages/Dapr.EntityFrameworkCore.Outbox/src/Dapr.EntityFrameworkCore.Outbox/
COPY packages/JobEngine.Contracts/src/JobEngine.Contracts/JobEngine.Contracts.csproj packages/JobEngine.Contracts/src/JobEngine.Contracts/
COPY services/JobService/src/JobService.Domain/JobService.Domain.csproj services/JobService/src/JobService.Domain/
COPY services/JobService/src/JobService.Application/JobService.Application.csproj services/JobService/src/JobService.Application/
COPY services/JobService/src/JobService.Infrastructure/JobService.Infrastructure.csproj services/JobService/src/JobService.Infrastructure/
COPY services/JobService/src/JobService.Api/JobService.Api.csproj services/JobService/src/JobService.Api/
RUN dotnet restore services/JobService/src/JobService.Api/JobService.Api.csproj

COPY packages/ packages/
COPY services/JobService/ services/JobService/
RUN dotnet publish services/JobService/src/JobService.Api/JobService.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000
ENTRYPOINT ["dotnet", "JobService.Api.dll"]

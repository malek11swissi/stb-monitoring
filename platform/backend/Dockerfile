FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY StbMonitoring.sln global.json NuGet.Config ./
COPY platform/backend/StbMonitoring.Domain/StbMonitoring.Domain.csproj platform/backend/StbMonitoring.Domain/
COPY platform/backend/StbMonitoring.Application/StbMonitoring.Application.csproj platform/backend/StbMonitoring.Application/
COPY platform/backend/StbMonitoring.Infrastructure/StbMonitoring.Infrastructure.csproj platform/backend/StbMonitoring.Infrastructure/
COPY platform/backend/StbMonitoring.Api/StbMonitoring.Api.csproj platform/backend/StbMonitoring.Api/
RUN dotnet restore platform/backend/StbMonitoring.Api/StbMonitoring.Api.csproj

COPY platform/backend platform/backend
RUN dotnet publish platform/backend/StbMonitoring.Api/StbMonitoring.Api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
RUN addgroup --system --gid 10001 stb && adduser --system --uid 10001 --ingroup stb stb \
    && mkdir -p /app/wwwroot/avatars /app/wwwroot/attachments \
    && chown -R stb:stb /app
COPY --from=build --chown=stb:stb /app/publish .
USER 10001
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
ENTRYPOINT ["dotnet", "StbMonitoring.Api.dll"]

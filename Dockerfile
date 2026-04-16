FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["Scheduler.sln", "./"]
COPY ["src/Scheduler.App/Scheduler.App.csproj", "src/Scheduler.App/"]
COPY ["src/Scheduler.Application/Scheduler.Application.csproj", "src/Scheduler.Application/"]
COPY ["src/Scheduler.Contracts/Scheduler.Contracts.csproj", "src/Scheduler.Contracts/"]
COPY ["src/Scheduler.Domain/Scheduler.Domain.csproj", "src/Scheduler.Domain/"]
COPY ["src/Scheduler.Infrastructure/Scheduler.Infrastructure.csproj", "src/Scheduler.Infrastructure/"]
RUN dotnet restore "src/Scheduler.App/Scheduler.App.csproj"

COPY . .
RUN dotnet publish "src/Scheduler.App/Scheduler.App.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

RUN mkdir -p /app/data
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Scheduler.App.dll"]

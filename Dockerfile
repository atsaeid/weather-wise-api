FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/WeatherWise.Api/WeatherWise.Api.csproj", "src/WeatherWise.Api/"]
COPY ["src/WeatherWise.Application/WeatherWise.Application.csproj", "src/WeatherWise.Application/"]
COPY ["src/WeatherWise.Domain/WeatherWise.Domain.csproj", "src/WeatherWise.Domain/"]
COPY ["src/WeatherWise.Infrastructure/WeatherWise.Infrastructure.csproj", "src/WeatherWise.Infrastructure/"]
RUN dotnet restore "src/WeatherWise.Api/WeatherWise.Api.csproj"
COPY . .
WORKDIR "/src/src/WeatherWise.Api"
RUN dotnet build "WeatherWise.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WeatherWise.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
RUN mkdir -p Data
ENTRYPOINT ["dotnet", "WeatherWise.Api.dll"] 
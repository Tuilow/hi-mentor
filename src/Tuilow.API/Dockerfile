FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/Tuilow.API/Tuilow.API.csproj", "src/Tuilow.API/"]
COPY ["src/Tuilow.Application/Tuilow.Application.csproj", "src/Tuilow.Application/"]
COPY ["src/Tuilow.Domain/Tuilow.Domain.csproj", "src/Tuilow.Domain/"]
COPY ["src/Tuilow.Infrastructure/Tuilow.Infrastructure.csproj", "src/Tuilow.Infrastructure/"]
RUN dotnet restore "src/Tuilow.API/Tuilow.API.csproj"
COPY . .
WORKDIR "/src/src/Tuilow.API"
RUN dotnet build "Tuilow.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Tuilow.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Tuilow.API.dll"]

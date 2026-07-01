FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/DogMaster.API/DogMaster.API.csproj", "src/DogMaster.API/"]
COPY ["src/DogMaster.Application/DogMaster.Application.csproj", "src/DogMaster.Application/"]
COPY ["src/DogMaster.Domain/DogMaster.Domain.csproj", "src/DogMaster.Domain/"]
COPY ["src/DogMaster.Infrastructure/DogMaster.Infrastructure.csproj", "src/DogMaster.Infrastructure/"]
RUN dotnet restore "src/DogMaster.API/DogMaster.API.csproj"
COPY . .
WORKDIR "/src/src/DogMaster.API"
RUN dotnet build "DogMaster.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DogMaster.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DogMaster.API.dll"]

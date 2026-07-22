FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# yt-dlp + ffmpeg — usados pelo YouTubeDownloadWorker (Streaming) para baixar vídeos do YouTube
# quando o criador marca a opção "baixar vídeo" no passo 2 do assistente. ffmpeg via apt (junta
# áudio+vídeo quando o yt-dlp baixa formatos separados); yt-dlp via binário standalone oficial
# (não depende de Python instalado separadamente).
RUN apt-get update \
    && apt-get install -y --no-install-recommends ffmpeg ca-certificates curl \
    && curl -L https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp -o /usr/local/bin/yt-dlp \
    && chmod a+rx /usr/local/bin/yt-dlp \
    && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# SharedKernel
COPY ["SharedKernel/Tuilow.SharedKernel.Domain/Tuilow.SharedKernel.Domain.csproj", "SharedKernel/Tuilow.SharedKernel.Domain/"]
COPY ["SharedKernel/Tuilow.SharedKernel.Application/Tuilow.SharedKernel.Application.csproj", "SharedKernel/Tuilow.SharedKernel.Application/"]
COPY ["SharedKernel/Tuilow.SharedKernel.Infrastructure/Tuilow.SharedKernel.Infrastructure.csproj", "SharedKernel/Tuilow.SharedKernel.Infrastructure/"]

# Modules/IdentidadeAcesso
COPY ["Modules/IdentidadeAcesso/Tuilow.IdentidadeAcesso.Domain/Tuilow.IdentidadeAcesso.Domain.csproj", "Modules/IdentidadeAcesso/Tuilow.IdentidadeAcesso.Domain/"]
COPY ["Modules/IdentidadeAcesso/Tuilow.IdentidadeAcesso.Application/Tuilow.IdentidadeAcesso.Application.csproj", "Modules/IdentidadeAcesso/Tuilow.IdentidadeAcesso.Application/"]
COPY ["Modules/IdentidadeAcesso/Tuilow.IdentidadeAcesso.Infrastructure/Tuilow.IdentidadeAcesso.Infrastructure.csproj", "Modules/IdentidadeAcesso/Tuilow.IdentidadeAcesso.Infrastructure/"]
COPY ["Modules/IdentidadeAcesso/Tuilow.IdentidadeAcesso.Api/Tuilow.IdentidadeAcesso.Api.csproj", "Modules/IdentidadeAcesso/Tuilow.IdentidadeAcesso.Api/"]

# Modules/Catalog
COPY ["Modules/Catalog/Tuilow.Catalog.Domain/Tuilow.Catalog.Domain.csproj", "Modules/Catalog/Tuilow.Catalog.Domain/"]
COPY ["Modules/Catalog/Tuilow.Catalog.Application/Tuilow.Catalog.Application.csproj", "Modules/Catalog/Tuilow.Catalog.Application/"]
COPY ["Modules/Catalog/Tuilow.Catalog.Infrastructure/Tuilow.Catalog.Infrastructure.csproj", "Modules/Catalog/Tuilow.Catalog.Infrastructure/"]
COPY ["Modules/Catalog/Tuilow.Catalog.Api/Tuilow.Catalog.Api.csproj", "Modules/Catalog/Tuilow.Catalog.Api/"]

# Modules/Learning
COPY ["Modules/Learning/Tuilow.Learning.Domain/Tuilow.Learning.Domain.csproj", "Modules/Learning/Tuilow.Learning.Domain/"]
COPY ["Modules/Learning/Tuilow.Learning.Application/Tuilow.Learning.Application.csproj", "Modules/Learning/Tuilow.Learning.Application/"]
COPY ["Modules/Learning/Tuilow.Learning.Infrastructure/Tuilow.Learning.Infrastructure.csproj", "Modules/Learning/Tuilow.Learning.Infrastructure/"]
COPY ["Modules/Learning/Tuilow.Learning.Api/Tuilow.Learning.Api.csproj", "Modules/Learning/Tuilow.Learning.Api/"]

# Modules/Journey
COPY ["Modules/Journey/Tuilow.Journey.Domain/Tuilow.Journey.Domain.csproj", "Modules/Journey/Tuilow.Journey.Domain/"]
COPY ["Modules/Journey/Tuilow.Journey.Application/Tuilow.Journey.Application.csproj", "Modules/Journey/Tuilow.Journey.Application/"]
COPY ["Modules/Journey/Tuilow.Journey.Infrastructure/Tuilow.Journey.Infrastructure.csproj", "Modules/Journey/Tuilow.Journey.Infrastructure/"]
COPY ["Modules/Journey/Tuilow.Journey.Api/Tuilow.Journey.Api.csproj", "Modules/Journey/Tuilow.Journey.Api/"]

# Modules/Sales
COPY ["Modules/Sales/Tuilow.Sales.Domain/Tuilow.Sales.Domain.csproj", "Modules/Sales/Tuilow.Sales.Domain/"]
COPY ["Modules/Sales/Tuilow.Sales.Application/Tuilow.Sales.Application.csproj", "Modules/Sales/Tuilow.Sales.Application/"]
COPY ["Modules/Sales/Tuilow.Sales.Infrastructure/Tuilow.Sales.Infrastructure.csproj", "Modules/Sales/Tuilow.Sales.Infrastructure/"]
COPY ["Modules/Sales/Tuilow.Sales.Api/Tuilow.Sales.Api.csproj", "Modules/Sales/Tuilow.Sales.Api/"]

# Modules/Streaming
COPY ["Modules/Streaming/Tuilow.Streaming.Domain/Tuilow.Streaming.Domain.csproj", "Modules/Streaming/Tuilow.Streaming.Domain/"]
COPY ["Modules/Streaming/Tuilow.Streaming.Application/Tuilow.Streaming.Application.csproj", "Modules/Streaming/Tuilow.Streaming.Application/"]
COPY ["Modules/Streaming/Tuilow.Streaming.Infrastructure/Tuilow.Streaming.Infrastructure.csproj", "Modules/Streaming/Tuilow.Streaming.Infrastructure/"]
COPY ["Modules/Streaming/Tuilow.Streaming.Api/Tuilow.Streaming.Api.csproj", "Modules/Streaming/Tuilow.Streaming.Api/"]

# Modules/Finance
COPY ["Modules/Finance/Tuilow.Finance.Domain/Tuilow.Finance.Domain.csproj", "Modules/Finance/Tuilow.Finance.Domain/"]
COPY ["Modules/Finance/Tuilow.Finance.Application/Tuilow.Finance.Application.csproj", "Modules/Finance/Tuilow.Finance.Application/"]
COPY ["Modules/Finance/Tuilow.Finance.Infrastructure/Tuilow.Finance.Infrastructure.csproj", "Modules/Finance/Tuilow.Finance.Infrastructure/"]
COPY ["Modules/Finance/Tuilow.Finance.Api/Tuilow.Finance.Api.csproj", "Modules/Finance/Tuilow.Finance.Api/"]

# Modules/Payout
COPY ["Modules/Payout/Tuilow.Payout.Domain/Tuilow.Payout.Domain.csproj", "Modules/Payout/Tuilow.Payout.Domain/"]
COPY ["Modules/Payout/Tuilow.Payout.Application/Tuilow.Payout.Application.csproj", "Modules/Payout/Tuilow.Payout.Application/"]
COPY ["Modules/Payout/Tuilow.Payout.Infrastructure/Tuilow.Payout.Infrastructure.csproj", "Modules/Payout/Tuilow.Payout.Infrastructure/"]
COPY ["Modules/Payout/Tuilow.Payout.Api/Tuilow.Payout.Api.csproj", "Modules/Payout/Tuilow.Payout.Api/"]

# Modules/CreatorStudio
COPY ["Modules/CreatorStudio/Tuilow.CreatorStudio.Domain/Tuilow.CreatorStudio.Domain.csproj", "Modules/CreatorStudio/Tuilow.CreatorStudio.Domain/"]
COPY ["Modules/CreatorStudio/Tuilow.CreatorStudio.Application/Tuilow.CreatorStudio.Application.csproj", "Modules/CreatorStudio/Tuilow.CreatorStudio.Application/"]
COPY ["Modules/CreatorStudio/Tuilow.CreatorStudio.Infrastructure/Tuilow.CreatorStudio.Infrastructure.csproj", "Modules/CreatorStudio/Tuilow.CreatorStudio.Infrastructure/"]
COPY ["Modules/CreatorStudio/Tuilow.CreatorStudio.Api/Tuilow.CreatorStudio.Api.csproj", "Modules/CreatorStudio/Tuilow.CreatorStudio.Api/"]

# Modules/Channel
COPY ["Modules/Channel/Tuilow.Channel.Domain/Tuilow.Channel.Domain.csproj", "Modules/Channel/Tuilow.Channel.Domain/"]
COPY ["Modules/Channel/Tuilow.Channel.Application/Tuilow.Channel.Application.csproj", "Modules/Channel/Tuilow.Channel.Application/"]
COPY ["Modules/Channel/Tuilow.Channel.Infrastructure/Tuilow.Channel.Infrastructure.csproj", "Modules/Channel/Tuilow.Channel.Infrastructure/"]
COPY ["Modules/Channel/Tuilow.Channel.Api/Tuilow.Channel.Api.csproj", "Modules/Channel/Tuilow.Channel.Api/"]

# Host
COPY ["Host/Tuilow.Host.Api/Tuilow.Host.Api.csproj", "Host/Tuilow.Host.Api/"]

RUN dotnet restore "Host/Tuilow.Host.Api/Tuilow.Host.Api.csproj"
COPY . .
WORKDIR "/src/Host/Tuilow.Host.Api"
RUN dotnet build "Tuilow.Host.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Tuilow.Host.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Tuilow.Host.Api.dll"]

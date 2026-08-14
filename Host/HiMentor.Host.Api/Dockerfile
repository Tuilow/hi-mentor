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
    && curl -L https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp_linux -o /usr/local/bin/yt-dlp \
    && chmod a+rx /usr/local/bin/yt-dlp \
    && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# SharedKernel
COPY ["SharedKernel/HiMentor.SharedKernel.Domain/HiMentor.SharedKernel.Domain.csproj", "SharedKernel/HiMentor.SharedKernel.Domain/"]
COPY ["SharedKernel/HiMentor.SharedKernel.Application/HiMentor.SharedKernel.Application.csproj", "SharedKernel/HiMentor.SharedKernel.Application/"]
COPY ["SharedKernel/HiMentor.SharedKernel.Infrastructure/HiMentor.SharedKernel.Infrastructure.csproj", "SharedKernel/HiMentor.SharedKernel.Infrastructure/"]

# Modules/IdentidadeAcesso
COPY ["Modules/IdentidadeAcesso/HiMentor.IdentidadeAcesso.Domain/HiMentor.IdentidadeAcesso.Domain.csproj", "Modules/IdentidadeAcesso/HiMentor.IdentidadeAcesso.Domain/"]
COPY ["Modules/IdentidadeAcesso/HiMentor.IdentidadeAcesso.Application/HiMentor.IdentidadeAcesso.Application.csproj", "Modules/IdentidadeAcesso/HiMentor.IdentidadeAcesso.Application/"]
COPY ["Modules/IdentidadeAcesso/HiMentor.IdentidadeAcesso.Infrastructure/HiMentor.IdentidadeAcesso.Infrastructure.csproj", "Modules/IdentidadeAcesso/HiMentor.IdentidadeAcesso.Infrastructure/"]
COPY ["Modules/IdentidadeAcesso/HiMentor.IdentidadeAcesso.Api/HiMentor.IdentidadeAcesso.Api.csproj", "Modules/IdentidadeAcesso/HiMentor.IdentidadeAcesso.Api/"]

# Modules/Catalog
COPY ["Modules/Catalog/HiMentor.Catalog.Domain/HiMentor.Catalog.Domain.csproj", "Modules/Catalog/HiMentor.Catalog.Domain/"]
COPY ["Modules/Catalog/HiMentor.Catalog.Application/HiMentor.Catalog.Application.csproj", "Modules/Catalog/HiMentor.Catalog.Application/"]
COPY ["Modules/Catalog/HiMentor.Catalog.Infrastructure/HiMentor.Catalog.Infrastructure.csproj", "Modules/Catalog/HiMentor.Catalog.Infrastructure/"]
COPY ["Modules/Catalog/HiMentor.Catalog.Api/HiMentor.Catalog.Api.csproj", "Modules/Catalog/HiMentor.Catalog.Api/"]

# Modules/Learning
COPY ["Modules/Learning/HiMentor.Learning.Domain/HiMentor.Learning.Domain.csproj", "Modules/Learning/HiMentor.Learning.Domain/"]
COPY ["Modules/Learning/HiMentor.Learning.Application/HiMentor.Learning.Application.csproj", "Modules/Learning/HiMentor.Learning.Application/"]
COPY ["Modules/Learning/HiMentor.Learning.Infrastructure/HiMentor.Learning.Infrastructure.csproj", "Modules/Learning/HiMentor.Learning.Infrastructure/"]
COPY ["Modules/Learning/HiMentor.Learning.Api/HiMentor.Learning.Api.csproj", "Modules/Learning/HiMentor.Learning.Api/"]

# Modules/Journey
COPY ["Modules/Journey/HiMentor.Journey.Domain/HiMentor.Journey.Domain.csproj", "Modules/Journey/HiMentor.Journey.Domain/"]
COPY ["Modules/Journey/HiMentor.Journey.Application/HiMentor.Journey.Application.csproj", "Modules/Journey/HiMentor.Journey.Application/"]
COPY ["Modules/Journey/HiMentor.Journey.Infrastructure/HiMentor.Journey.Infrastructure.csproj", "Modules/Journey/HiMentor.Journey.Infrastructure/"]
COPY ["Modules/Journey/HiMentor.Journey.Api/HiMentor.Journey.Api.csproj", "Modules/Journey/HiMentor.Journey.Api/"]

# Modules/Sales
COPY ["Modules/Sales/HiMentor.Sales.Domain/HiMentor.Sales.Domain.csproj", "Modules/Sales/HiMentor.Sales.Domain/"]
COPY ["Modules/Sales/HiMentor.Sales.Application/HiMentor.Sales.Application.csproj", "Modules/Sales/HiMentor.Sales.Application/"]
COPY ["Modules/Sales/HiMentor.Sales.Infrastructure/HiMentor.Sales.Infrastructure.csproj", "Modules/Sales/HiMentor.Sales.Infrastructure/"]
COPY ["Modules/Sales/HiMentor.Sales.Api/HiMentor.Sales.Api.csproj", "Modules/Sales/HiMentor.Sales.Api/"]

# Modules/Streaming
COPY ["Modules/Streaming/HiMentor.Streaming.Domain/HiMentor.Streaming.Domain.csproj", "Modules/Streaming/HiMentor.Streaming.Domain/"]
COPY ["Modules/Streaming/HiMentor.Streaming.Application/HiMentor.Streaming.Application.csproj", "Modules/Streaming/HiMentor.Streaming.Application/"]
COPY ["Modules/Streaming/HiMentor.Streaming.Infrastructure/HiMentor.Streaming.Infrastructure.csproj", "Modules/Streaming/HiMentor.Streaming.Infrastructure/"]
COPY ["Modules/Streaming/HiMentor.Streaming.Api/HiMentor.Streaming.Api.csproj", "Modules/Streaming/HiMentor.Streaming.Api/"]

# Modules/Finance
COPY ["Modules/Finance/HiMentor.Finance.Domain/HiMentor.Finance.Domain.csproj", "Modules/Finance/HiMentor.Finance.Domain/"]
COPY ["Modules/Finance/HiMentor.Finance.Application/HiMentor.Finance.Application.csproj", "Modules/Finance/HiMentor.Finance.Application/"]
COPY ["Modules/Finance/HiMentor.Finance.Infrastructure/HiMentor.Finance.Infrastructure.csproj", "Modules/Finance/HiMentor.Finance.Infrastructure/"]
COPY ["Modules/Finance/HiMentor.Finance.Api/HiMentor.Finance.Api.csproj", "Modules/Finance/HiMentor.Finance.Api/"]

# Modules/Payout
COPY ["Modules/Payout/HiMentor.Payout.Domain/HiMentor.Payout.Domain.csproj", "Modules/Payout/HiMentor.Payout.Domain/"]
COPY ["Modules/Payout/HiMentor.Payout.Application/HiMentor.Payout.Application.csproj", "Modules/Payout/HiMentor.Payout.Application/"]
COPY ["Modules/Payout/HiMentor.Payout.Infrastructure/HiMentor.Payout.Infrastructure.csproj", "Modules/Payout/HiMentor.Payout.Infrastructure/"]
COPY ["Modules/Payout/HiMentor.Payout.Api/HiMentor.Payout.Api.csproj", "Modules/Payout/HiMentor.Payout.Api/"]

# Modules/CreatorStudio
COPY ["Modules/CreatorStudio/HiMentor.CreatorStudio.Domain/HiMentor.CreatorStudio.Domain.csproj", "Modules/CreatorStudio/HiMentor.CreatorStudio.Domain/"]
COPY ["Modules/CreatorStudio/HiMentor.CreatorStudio.Application/HiMentor.CreatorStudio.Application.csproj", "Modules/CreatorStudio/HiMentor.CreatorStudio.Application/"]
COPY ["Modules/CreatorStudio/HiMentor.CreatorStudio.Infrastructure/HiMentor.CreatorStudio.Infrastructure.csproj", "Modules/CreatorStudio/HiMentor.CreatorStudio.Infrastructure/"]
COPY ["Modules/CreatorStudio/HiMentor.CreatorStudio.Api/HiMentor.CreatorStudio.Api.csproj", "Modules/CreatorStudio/HiMentor.CreatorStudio.Api/"]

# Modules/Channel
COPY ["Modules/Channel/HiMentor.Channel.Domain/HiMentor.Channel.Domain.csproj", "Modules/Channel/HiMentor.Channel.Domain/"]
COPY ["Modules/Channel/HiMentor.Channel.Application/HiMentor.Channel.Application.csproj", "Modules/Channel/HiMentor.Channel.Application/"]
COPY ["Modules/Channel/HiMentor.Channel.Infrastructure/HiMentor.Channel.Infrastructure.csproj", "Modules/Channel/HiMentor.Channel.Infrastructure/"]
COPY ["Modules/Channel/HiMentor.Channel.Api/HiMentor.Channel.Api.csproj", "Modules/Channel/HiMentor.Channel.Api/"]

# Host
COPY ["Host/HiMentor.Host.Api/HiMentor.Host.Api.csproj", "Host/HiMentor.Host.Api/"]

RUN dotnet restore "Host/HiMentor.Host.Api/HiMentor.Host.Api.csproj"
COPY . .
WORKDIR "/src/Host/HiMentor.Host.Api"
RUN dotnet build "HiMentor.Host.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "HiMentor.Host.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HiMentor.Host.Api.dll"]

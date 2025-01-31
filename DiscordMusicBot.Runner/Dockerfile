# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/runtime:9.0-noble AS base
USER root
WORKDIR /app
RUN apt-get update

RUN apt-get install software-properties-common -y
RUN apt-get install ffmpeg -y
RUN apt-get install libopus-dev -y
RUN apt-get install libsodium-dev -y

RUN add-apt-repository ppa:tomtomtom/yt-dlp
RUN apt-get install yt-dlp -y

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:9.0-noble AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Build.props", "."]
COPY ["DiscordMusicBot.Runner/DiscordMusicBot.Runner.csproj", "DiscordMusicBot.Runner/"]
COPY ["DiscordMusicBot.Client/DiscordMusicBot.Client.csproj", "DiscordMusicBot.Client/"]
COPY ["DiscordMusicBot.Core/DiscordMusicBot.Core.csproj", "DiscordMusicBot.Core/"]
RUN dotnet restore "./DiscordMusicBot.Runner/DiscordMusicBot.Runner.csproj"
COPY . .
WORKDIR "/src/DiscordMusicBot.Runner"
RUN dotnet build "./DiscordMusicBot.Runner.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./DiscordMusicBot.Runner.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

VOLUME /app/resources

ENTRYPOINT ["dotnet", "DiscordMusicBot.Runner.dll"]
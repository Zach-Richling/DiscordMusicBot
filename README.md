# DiscordMusicBot

## Setup
appsettings.json must be created in DiscordMusicBot.Runner root folder including the following:
- BotToken: Token used to authenticate with discord bot user. --Required
- YoutubeEmoji: Identifier for youtube discord emoji. String of the form "<":EmojiName:EmojiId>" --Optional
- SoundCloudEmoji: Identifier for soundcloud discord emoji. String of the form "<":EmojiName:EmojiId>" --Optional
- SpotifyEmoji: Identifier for spotify discord emoji. String of the form "<":EmojiName:EmojiId>" --Optional
- BandcampEmoji: Identifier for bandcamp discord emoji. String of the form "<":EmojiName:EmojiId>" --Optional
- AppleEmoji: Identifier for apple discord emoji. String of the form "<":EmojiName:EmojiId>" --Optional
- FalAI_APIKey: API key to authenticate with the FALAI API. Used for AI image generation. --Optional
- SoundCloudClientId: Client Id used to authenticate with the Sound Cloud API. --Optional

## Publish and Deployment
### Windows
- Publish DiscordMusicBot.Runner project.
- Copy all files from ~\DiscordMusicBot\DiscordMusicBot.Runner\bin\Release\net6.0\publish\win-x64 to the computer that will host the bot.
- Run DiscordMusicBot.Runner.exe to start the bot.

### Docker
- A docker image can be found [here](https://hub.docker.com/r/puddlebuddy/puddle-bot).
- It comes with all dependencies installed.
- You must mount the appsettings file into the container in the /app/resources directory.

## Dependencies
### FFMPEG
- This bot requires FFMPEG to be installed and included in the PATH of the computer running the bot. [Install from here](https://ffmpeg.org/download.html)
- FFMPEG requires two additional DLLs to be present. libsodium.dll and opus.dll. [Binaries can be downloaded here](https://github.com/discord-net/Discord.Net/tree/dev/voice-natives)

## Further Development
- Add playlist starting at certain index
- Pause, Resume, Repeat, and Stop (Saving the current queue)
- Clean up resources if guild is inactive after certain period of time

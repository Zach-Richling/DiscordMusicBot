# DiscordMusicBot

## Setup
appsettings.json must be created in DiscordMusicBot.Runner root folder including the following:
- BotToken: Token used to authenticate with discord bot user.
- YoutubeEmoji: Identifier for youtube discord emoji. String of the form "<"EmojiName:EmojiId>"
- SoundCloudEmoji: Identifier for soundcloud discord emoji. String of the form "<"EmojiName:EmojiId>"
- SpotifyEmoji: Identifier for spotify discord emoji. String of the form "<"EmojiName:EmojiId>"

## Publish and Deployment
### Windows
- Publish DiscordMusicBot.Runner project.
- Copy all files from ~\DiscordMusicBot\DiscordMusicBot.Runner\bin\Release\net6.0\publish\win-x64 to the computer that will host the bot.
- Run DiscordMusicBot.Runner.exe to start the bot.

## Dependencies
### FFMPEG
- This bot requires FFMPEG to be installed and included in the PATH of the computer running the bot. [Install from here](https://ffmpeg.org/download.html)
- FFMPEG requires two additional DLLs to be present. libsodium.dll and opus.dll. [Binaries can be downloaded here](https://github.com/discord-net/Discord.Net/tree/dev/voice-natives)

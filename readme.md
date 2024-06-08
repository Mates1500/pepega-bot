Inhouse (in-server?) joke Discord bot. Unless you're already aware of what this is, you likely won't care about this project.
# How to use
1. Create `config.secret.json` in the VolData folder (`pepega-bot/VolData`)
2. Fill it with the bot secret
```json
{
    "BotSecret": "GO TO https://discord.com/developers/applications -> Your App -> Bot -> Click to Reveal Token. Paste here"
}
```
3. Run with `dotnet run` or F5 in Visual Studio

## Docker (Linux)
You can find the compiled image [here](https://hub.docker.com/r/mates1500/pepega-bot). This repository uses Github actions for CI on every push to the `master` branch.

Of course, you can also build the image yourself using the repository's `Dockerfile`.

Use an example `docker-compose` file like this. Use either a bind, or named docker volume, but bind it to `/app/VolData`. It contains `config.json`, `config.secret.json` and the text `logs` folder by default
```yml
services:
  pepega-bot:
    image: mates1500/pepega-bot:latest
    volumes:
      - pepega-bot-vol:/app/VolData
    network_mode: "host"
    restart: unless-stopped
volumes:
  pepega-bot-vol:

```
Currently, `amd64` and `arm64` linux architectures are supported and automatically built in the CI pipeline.

Windows containers should also theoretically work with a different base image, but I don't care enough with my usecase to test it out.
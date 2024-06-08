FROM mcr.microsoft.com/dotnet/sdk:8.0 as build
COPY pepega-bot/* /pepega-bot/
WORKDIR /pepega-bot
RUN dotnet publish -c debug -o /app-build

FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app-build ./
ENTRYPOINT ["dotnet", "pepega-bot.dll"]

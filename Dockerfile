FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 as build
ARG TARGETARCH
COPY pepega-bot/* /pepega-bot/
WORKDIR /pepega-bot
RUN dotnet publish -a $TARGETARCH -c debug -o /app-build

FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app-build ./
ENTRYPOINT ["dotnet", "pepega-bot.dll"]

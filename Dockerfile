FROM cgr.dev/chainguard/dotnet-sdk:latest-dev

WORKDIR /app
COPY out/ /app/

ENV ASPNETCORE_URLS=http://0.0.0.0:7020

ENTRYPOINT ["dotnet", "PlaygroundService.dll"]

EXPOSE 7020
FROM mcr.microsoft.com/dotnet/aspnet:7.0

WORKDIR /app
COPY out/ /app/

ENV ASPNETCORE_URLS=http://0.0.0.0:7020

ENTRYPOINT ["dotnet", "PlaygroundService.dll"]

EXPOSE 7020
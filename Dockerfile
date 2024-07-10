FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build-env

WORKDIR /app
COPY out/ /app/

ENV ASPNETCORE_URLS=http://0.0.0.0:7020

RUN dotnet new --install AElf.ContractTemplates

ENTRYPOINT ["dotnet", "PlaygroundService.dll"]

EXPOSE 7020
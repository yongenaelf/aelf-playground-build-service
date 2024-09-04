# Use .NET 7 SDK as the base for building the project
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build-env

# Set the working directory
WORKDIR /app

# Copy the project files into the container
COPY out/ /app/

# Install .NET 6 SDK and runtime for running the tests, alongside .NET 7 SDK
COPY --from=mcr.microsoft.com/dotnet/sdk:6.0 /usr/share/dotnet/sdk /usr/share/dotnet/sdk
COPY --from=mcr.microsoft.com/dotnet/sdk:6.0 /usr/share/dotnet/shared /usr/share/dotnet/shared

# Set environment variables, expose required port
ENV ASPNETCORE_URLS=http://0.0.0.0:7020

# Install the AElf.ContractTemplates globally
RUN dotnet new --install AElf.ContractTemplates

# Entry point to run the service
ENTRYPOINT ["dotnet", "PlaygroundService.dll"]

# Expose port 7020
EXPOSE 7020

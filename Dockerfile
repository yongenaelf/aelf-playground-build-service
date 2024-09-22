ARG DOTNET_6=6.0.410
# Use .NET 6 SDK as the primary environment
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_6} AS base-env

# Set the working directory
WORKDIR /app

# Install .NET 7 Runtime alongside .NET 6 SDK
COPY --from=mcr.microsoft.com/dotnet/sdk:7.0 /usr/share/dotnet/shared /usr/share/dotnet/shared

# Install ClamAV
RUN apt-get update && apt-get install -y clamav clamav-daemon

# Copy the project files
COPY out/ /app/

# Ensure ClamAV has the latest definitions before each container run
RUN freshclam

# Startup script to update ClamAV definitions and start the service
RUN echo '#!/bin/bash\nfreshclam\n/usr/share/dotnet/dotnet PlaygroundService.dll' > /app/start.sh \
    && chmod +x /app/start.sh

# Set environment variables for the service
ENV ASPNETCORE_URLS=http://0.0.0.0:7020

# Install the AElf.ContractTemplates globally
RUN dotnet new --install AElf.ContractTemplates

# Create global.json to force the use of .NET 6 SDK
RUN dotnet new globaljson --sdk-version $$DOTNET_6 --force

# Use the startup script as the entry point
ENTRYPOINT ["/app/start.sh"]

# Expose port 7020
EXPOSE 7020

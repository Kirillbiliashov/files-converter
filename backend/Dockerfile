# Use .NET SDK for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
COPY ["backend.csproj", "./"]
RUN dotnet restore "backend.csproj"
COPY . .
RUN dotnet build "backend.csproj" -c Release -o /app/build

# Publish the app
FROM build AS publish
RUN dotnet publish "backend.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Use .NET Runtime to run the app
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install LibreOffice
RUN apt-get update && apt-get install -y libreoffice

# Copy published .NET app
COPY --from=publish /app/publish .

# Set entry point
ENTRYPOINT ["dotnet", "backend.dll"]

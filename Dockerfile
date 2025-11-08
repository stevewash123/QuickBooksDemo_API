# Use the official .NET 8 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy solution file and all project files
COPY *.sln .
COPY QuickBooksDemo.Api/*.csproj ./QuickBooksDemo.Api/
COPY QuickBooksDemo.DAL/*.csproj ./QuickBooksDemo.DAL/
COPY QuickBooksDemo.Models/*.csproj ./QuickBooksDemo.Models/
COPY QuickBooksDemo.Service/*.csproj ./QuickBooksDemo.Service/

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY . .

# Build and publish the application
RUN dotnet publish QuickBooksDemo.Api/QuickBooksDemo.Api.csproj -c Release -o /app/publish

# Use the official .NET 8 runtime image for running
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy the published application
COPY --from=build /app/publish .

# Expose port 8080 (Render's default)
EXPOSE 8080

# Set the entry point
ENTRYPOINT ["dotnet", "QuickBooksDemo.Api.dll"]
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY WebApp/WebApp/WebApp.csproj WebApp/WebApp/
RUN dotnet restore WebApp/WebApp/WebApp.csproj
COPY . .
RUN dotnet publish WebApp/WebApp/WebApp.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "WebApp.dll"]

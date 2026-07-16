FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY SupportAgent.API/SupportAgent.API.csproj SupportAgent.API/
RUN dotnet restore SupportAgent.API/SupportAgent.API.csproj
COPY . .
RUN dotnet publish SupportAgent.API/SupportAgent.API.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "SupportAgent.API.dll"]

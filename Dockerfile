FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/T20Master.Web/T20Master.Web.csproj src/T20Master.Web/
RUN dotnet restore src/T20Master.Web/T20Master.Web.csproj

COPY src/ src/
RUN dotnet publish src/T20Master.Web/T20Master.Web.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:${PORT:-8080}

EXPOSE 8080
ENTRYPOINT ["dotnet", "T20Master.Web.dll"]

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY backend backend
RUN dotnet restore backend/Krizovka.Api/Krizovka.Api.csproj
RUN dotnet publish backend/Krizovka.Api/Krizovka.Api.csproj -c Release -o /out --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out .
ENV ASPNETCORE_HTTP_PORTS=8080
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "Krizovka.Api.dll"]

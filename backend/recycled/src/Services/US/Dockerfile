FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY backend backend
RUN dotnet publish backend/recycled/src/Services/US/src/Krizovka.Users.Api/Krizovka.Users.Api.csproj -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out .
ENV ASPNETCORE_HTTP_PORTS=8080
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "Krizovka.Users.Api.dll"]

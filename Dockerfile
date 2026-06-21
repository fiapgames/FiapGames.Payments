FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY FiapGames.Payments/FiapGames.Payments.csproj FiapGames.Payments/
RUN dotnet restore FiapGames.Payments/FiapGames.Payments.csproj

COPY FiapGames.Payments/ FiapGames.Payments/
RUN dotnet publish FiapGames.Payments/FiapGames.Payments.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

EXPOSE 8080
ENTRYPOINT ["dotnet", "FiapGames.Payments.dll"]

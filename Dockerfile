# ---- Steg 1: Bygg ----
# Bruker .NET SDK-bildet, som har verktøyene som trengs for å bygge appen
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Kopierer prosjektfilen først og henter pakker.
# Docker husker dette steget, så pakkene lastes bare ned på nytt hvis .csproj endres.
COPY Nabohjelp.csproj ./
RUN dotnet restore Nabohjelp.csproj

# Kopierer resten av koden og bygger en ferdig versjon av appen.
# Prosjektfilen må nevnes, fordi mappen også har løsningsfilen Nabohjelp.slnx
# (da vet ikke dotnet hvilken av dem den skal bygge).
COPY . ./
RUN dotnet publish Nabohjelp.csproj -c Release -o /app/publish

# ---- Steg 2: Kjør ----
# Bruker det mindre ASP.NET-bildet, som bare kan kjøre appen.
# Byggeverktøyene fra steg 1 blir ikke med, så containeren blir mindre.
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# Henter den ferdige appen fra steg 1
COPY --from=build /app/publish .

# Appen lytter på port 8080 inne i containeren
EXPOSE 8080

# Starter appen når containeren startes
ENTRYPOINT ["dotnet", "Nabohjelp.dll"]
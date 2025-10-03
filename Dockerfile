# -------- Build stage --------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia todo y restaura
COPY . .
RUN dotnet restore

# Publica en Release
# (si tu .sln tiene 1 proyecto web basta; si no, especifica el .csproj)
RUN dotnet publish -c Release -o /app/out

# -------- Runtime stage --------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/out .

# Render inyecta PORT; tu Program.cs ya usa ese valor
ENV ASPNETCORE_ENVIRONMENT=Production

# Arranque
CMD ["dotnet", "GanaderiaControl.dll"]

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore
COPY Finstance.csproj .
RUN dotnet restore

# Copy everything else and build
COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

COPY --from=build /app/publish .

# Copy seed data files
COPY --from=build /src/categoryKeywords.json .
COPY --from=build /src/expenseLocationsSeed.json .

ENV ASPNETCORE_URLS=http://+:5257
EXPOSE 5257

ENTRYPOINT ["dotnet", "Finstance.dll"]

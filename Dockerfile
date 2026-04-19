FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ETL.sln ./
COPY Directory.Build.props ./
COPY src/ETL.Domain/ETL.Domain.csproj src/ETL.Domain/
COPY src/ETL.Application/ETL.Application.csproj src/ETL.Application/
COPY src/ETL.Infrastructure/ETL.Infrastructure.csproj src/ETL.Infrastructure/
COPY src/ETL.Web/ETL.Web.csproj src/ETL.Web/

RUN dotnet restore src/ETL.Web/ETL.Web.csproj

COPY src ./src
RUN dotnet publish src/ETL.Web/ETL.Web.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .
EXPOSE 8080

ENTRYPOINT ["dotnet", "ETL.Web.dll"]

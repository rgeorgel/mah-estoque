FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/api/MahEstoque.Api/MahEstoque.Api.csproj", "src/api/MahEstoque.Api/"]
RUN dotnet restore "src/api/MahEstoque.Api/MahEstoque.Api.csproj"

COPY src/api/MahEstoque.Api/ .
RUN dotnet publish "MahEstoque.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
RUN apt-get update && apt-get install -y --no-install-recommends libgssapi-krb5-2 && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
ENTRYPOINT ["dotnet", "MahEstoque.Api.dll"]
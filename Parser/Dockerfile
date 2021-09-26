FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["Parser/Parser.csproj", "Parser/"]
RUN dotnet restore "Parser/Parser.csproj"
COPY . .
WORKDIR "/src/Parser"
RUN dotnet build "Parser.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Parser.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Parser.dll"]

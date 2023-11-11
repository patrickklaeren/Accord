FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["src/Accord.Web/Accord.Web.csproj", "src/Accord.Web/"]
COPY ["src/Accord.Bot/Accord.Bot.csproj", "src/Accord.Bot/"]
COPY ["src/Accord.Services/Accord.Services.csproj", "src/Accord.Services/"]
COPY ["src/Accord.Domain/Accord.Domain.csproj", "src/Accord.Domain/"]
RUN dotnet restore "src/Accord.Web/Accord.Web.csproj"
COPY . .
WORKDIR "/src/src/Accord.Web"
RUN dotnet build "Accord.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Accord.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Accord.Web.dll"]

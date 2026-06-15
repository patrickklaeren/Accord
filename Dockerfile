FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]

COPY ["src/Accord.Web/Accord.Web.csproj", "src/Accord.Web/"]
COPY ["src/Accord.Bot/Accord.Bot.csproj", "src/Accord.Bot/"]
COPY ["src/Accord.Services/Accord.Services.csproj", "src/Accord.Services/"]
COPY ["src/Accord.Domain/Accord.Domain.csproj", "src/Accord.Domain/"]

RUN dotnet restore "src/Accord.Web/Accord.Web.csproj"

COPY . .

WORKDIR "/src/src/Accord.Web"
RUN dotnet publish "Accord.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Accord.Web.dll"]
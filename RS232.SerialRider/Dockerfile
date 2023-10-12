FROM mcr.microsoft.com/dotnet/runtime:7.0-jammy-amd64 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0-jammy-amd64 AS build
WORKDIR /src
COPY ["RS232.SerialRider/RS232.SerialRider.csproj", "RS232.SerialRider/"]
COPY ["RS232.Models/RS232.Models.csproj", "RS232.Models/"]
RUN dotnet restore "RS232.SerialRider/RS232.SerialRider.csproj"
COPY . .
WORKDIR "/src/RS232.SerialRider"
RUN dotnet build "RS232.SerialRider.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "RS232.SerialRider.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RS232.SerialRider.dll"]
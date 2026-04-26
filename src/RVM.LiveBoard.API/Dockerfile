FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY nuget.config .
COPY ["src/RVM.LiveBoard.API/RVM.LiveBoard.API.csproj", "src/RVM.LiveBoard.API/"]
COPY ["src/RVM.LiveBoard.Domain/RVM.LiveBoard.Domain.csproj", "src/RVM.LiveBoard.Domain/"]
COPY ["src/RVM.LiveBoard.Infrastructure/RVM.LiveBoard.Infrastructure.csproj", "src/RVM.LiveBoard.Infrastructure/"]
RUN dotnet restore "./src/RVM.LiveBoard.API/RVM.LiveBoard.API.csproj"
COPY . .
WORKDIR "/src/src/RVM.LiveBoard.API"
RUN dotnet build "./RVM.LiveBoard.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./RVM.LiveBoard.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RVM.LiveBoard.API.dll"]

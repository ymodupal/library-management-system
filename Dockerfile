#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM openfaas/of-watchdog:0.7.2 as watchdog

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["LightLib.Web/LightLib.Web.csproj", "LightLib.Web/"]
COPY ["LightLib.Models/LightLib.Models.csproj", "LightLib.Models/"]
COPY ["LightLib.Data/LightLib.Data.csproj", "LightLib.Data/"]
COPY ["LightLib.Service/LightLib.Service.csproj", "LightLib.Service/"]
RUN dotnet restore "LightLib.Web/LightLib.Web.csproj"
COPY . .
WORKDIR "/src/LightLib.Web"
RUN dotnet build "LightLib.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LightLib.Web.csproj" -c Release -o /app/publish

#FROM base AS final
#WORKDIR /app
#COPY --from=publish /app/publish .
#ENTRYPOINT ["dotnet", "LightLib.Web.dll"]

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=watchdog /fwatchdog /usr/bin/fwatchdog
RUN chmod +x /usr/bin/fwatchdog

ENV fprocess="dotnet LightLib.Web.dll"
ENV upstream_url="http://127.0.0.1:80"
ENV mode="http"

ENTRYPOINT ["fwatchdog"]
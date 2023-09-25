#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["WebApiProject2/WebApiProject2.csproj", "WebApiProject2/"]
RUN dotnet restore "WebApiProject2/WebApiProject2.csproj"
COPY . .
WORKDIR "/src/WebApiProject2"
RUN dotnet build "WebApiProject2.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WebApiProject2.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WebApiProject2.dll"]
#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0-alpine AS base
WORKDIR /app
#EXPOSE 80
EXPOSE 5000
ENV RABBITMQ_HOST localhost
ENV RABBITMQ_PORT 5672
ENV ASPNETCORE_URLS=http://+:5000

RUN adduser -u 5678 --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser


FROM mcr.microsoft.com/dotnet/sdk:5.0-alpine AS build
WORKDIR /src
COPY ["Task.csproj", "./"]
RUN dotnet restore "Task.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "Task.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Task.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Task.dll"]

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY dotpaste/*.csproj ./
RUN dotnet restore dotpaste.csproj

COPY dotpaste/ ./
RUN dotnet publish dotpaste.csproj --configuration Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine-composite as release
ARG DOTPASTE_UPLOADS_PATH=/app/uploads

WORKDIR /app

EXPOSE 8080/tcp
COPY --from=build /app ./

VOLUME $DOTPASTE_UPLOADS_PATH

ENTRYPOINT ["dotnet", "dotpaste.dll", "-u $DOTPASTE_UPLOADS_PATH"]
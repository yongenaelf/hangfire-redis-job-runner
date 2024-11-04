# Use the SDK image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS publish
ARG APPNAME
ENV APP=$APPNAME
WORKDIR /src
COPY "Shared/Shared.csproj" "Shared/"
COPY ${APP}/${APP}.csproj ${APP}/
RUN dotnet restore "./$APP/$APP.csproj"
COPY ${APP} ${APP}
COPY Shared Shared
RUN dotnet publish "$APP/$APP.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS processor
# copy shared libs from dotnet 6 sdk
COPY --from=mcr.microsoft.com/dotnet/sdk:6.0 /usr/share/dotnet /usr/share/dotnet
WORKDIR /app
COPY --from=publish /app/publish .
ENV DOTNET_CLI_USE_MSBUILD_SERVER=1
ENTRYPOINT ["dotnet", "HangfireJobProcessor.dll"]

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS api
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "JobEnqueuerService.dll"]
EXPOSE 8080
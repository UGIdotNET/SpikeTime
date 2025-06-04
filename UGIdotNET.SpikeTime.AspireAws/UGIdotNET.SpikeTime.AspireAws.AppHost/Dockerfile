FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG TARGETARCH
WORKDIR /src
COPY ["UGIdotNET.SpikeTime.AspireAws.AppHost/UGIdotNET.SpikeTime.AspireAws.AppHost.csproj", "UGIdotNET.SpikeTime.AspireAws.AppHost/"]
COPY ["UGIdotNET.SpikeTime.AspireAws.ServiceDefaults/UGIdotNET.SpikeTime.AspireAws.ServiceDefaults.csproj", "UGIdotNET.SpikeTime.AspireAws.ServiceDefaults/"]
COPY ["UGIdotNET.SpikeTime.AspireAws.ApiService/UGIdotNET.SpikeTime.AspireAws.ApiService.csproj", "UGIdotNET.SpikeTime.AspireAws.ApiService/"]
COPY ["UGIdotNET.SpikeTime.AspireAws.Web/UGIdotNET.SpikeTime.AspireAws.Web.csproj", "UGIdotNET.SpikeTime.AspireAws.Web/"]
COPY ["UGIdotNET.SpikeTime.AspireAws.Entities/UGIdotNET.SpikeTime.AspireAws.Entities.csproj", "UGIdotNET.SpikeTime.AspireAws.Entities/"]
COPY ["UGIdotNET.SpikeTime.AspireAws.Migrations/UGIdotNET.SpikeTime.AspireAws.Migrations.csproj", "UGIdotNET.SpikeTime.AspireAws.Migrations/"]
RUN dotnet restore "UGIdotNET.SpikeTime.AspireAws.AppHost/UGIdotNET.SpikeTime.AspireAws.AppHost.csproj" -a $TARGETARCH
COPY . .
WORKDIR "/src/UGIdotNET.SpikeTime.AspireAws.AppHost"
RUN dotnet build "UGIdotNET.SpikeTime.AspireAws.AppHost.csproj" -c Release -o /app/build -a $TARGETARCH

FROM build AS publish
RUN apt-get update -yq \
    && apt-get install -yq ca-certificates curl gnupg \
    && mkdir -p /etc/apt/keyrings \
    && curl -fsSL https://deb.nodesource.com/gpgkey/nodesource-repo.gpg.key | gpg --dearmor -o /etc/apt/keyrings/nodesource.gpg \
    && echo "deb [signed-by=/etc/apt/keyrings/nodesource.gpg] https://deb.nodesource.com/node_18.x nodistro main" | tee /etc/apt/sources.list.d/nodesource.list \
    && apt-get update -yq \
    && apt-get install nodejs -yq
RUN dotnet publish "UGIdotNET.SpikeTime.AspireAws.AppHost.csproj" -c Release -o /app/publish -a $TARGETARCH

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "UGIdotNET.SpikeTime.AspireAws.AppHost.dll"]

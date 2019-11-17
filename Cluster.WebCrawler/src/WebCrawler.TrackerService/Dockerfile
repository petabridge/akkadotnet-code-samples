FROM mcr.microsoft.com/dotnet/core/runtime:3.0 AS base
WORKDIR /app

# should be a comma-delimited list
ENV CLUSTER_SEEDS "[]"
ENV CLUSTER_IP ""
ENV CLUSTER_PORT "5212"

#Akka.Remote inbound listening endpoint
EXPOSE 5212

FROM mcr.microsoft.com/dotnet/core/sdk:3.0 AS build
WORKDIR /src
COPY *.sln ./
COPY docker-compose.* ./
COPY ./get-dockerip.sh ./get-dockerip.sh
COPY src/ src/
RUN dotnet restore WebCrawler.sln

COPY . .
WORKDIR /src/src/WebCrawler.TrackerService
RUN dotnet build -c Release -o /app

FROM build AS publish
RUN dotnet publish -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=build /src/get-dockerip.sh ./get-dockerip.sh
COPY --from=publish /app .

RUN apt-get update && apt-get install -y dos2unix
RUN dos2unix ./get-dockerip.sh

ENTRYPOINT ["/bin/bash","get-dockerip.sh"]

CMD ["dotnet", "WebCrawler.TrackerService.dll"]

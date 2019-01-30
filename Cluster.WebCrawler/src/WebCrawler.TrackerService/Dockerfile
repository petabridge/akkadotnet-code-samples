FROM microsoft/dotnet:2.2-runtime AS base
WORKDIR /app

# should be a comma-delimited list
ENV CLUSTER_SEEDS "[]"
ENV CLUSTER_IP ""
ENV CLUSTER_PORT "5212"

#Akka.Remote inbound listening endpoint
EXPOSE 5212 

FROM microsoft/dotnet:2.2-sdk AS build
WORKDIR /src
COPY . .
RUN dotnet restore WebCrawler.sln
WORKDIR /src/src/WebCrawler.TrackerService

RUN dotnet build -c Release -o /app

FROM build AS publish
RUN dotnet publish -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=build /src/get-dockerip.sh ./get-dockerip.sh
COPY --from=publish /app .

ENTRYPOINT ["/bin/bash","get-dockerip.sh"]

CMD ["dotnet", "WebCrawler.TrackerService.dll"]

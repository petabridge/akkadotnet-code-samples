FROM microsoft/dotnet:2.0-runtime AS base
WORKDIR /app

# should be a comma-delimited list
ENV CLUSTER_SEEDS "[]"
ENV CLUSTER_IP ""
ENV CLUSTER_PORT "5212"

#Akka.Remote inbound listening endpoint
EXPOSE 5212 

FROM microsoft/dotnet:2.0-sdk AS build
WORKDIR /src
COPY *.sln ./
COPY ./get-dockerip.sh ./get-dockerip.sh
COPY src/WebCrawler.TrackerService/WebCrawler.TrackerService.csproj src/WebCrawler.TrackerService/
COPY src/WebCrawler.Shared/WebCrawler.Shared.csproj src/WebCrawler.Shared/
COPY src/WebCrawler.Shared.IO/WebCrawler.Shared.IO.csproj src/WebCrawler.Shared.IO/
RUN dotnet restore
COPY . .
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

FROM microsoft/aspnetcore:2.0 AS base
WORKDIR /app

# should be a comma-delimited list
ENV CLUSTER_SEEDS "[]"
ENV CLUSTER_IP ""
ENV CLUSTER_PORT "16666"

EXPOSE 80

#Akka.Remote inbound listening endpoint
EXPOSE 16666 


FROM microsoft/aspnetcore-build:2.0 AS build
WORKDIR /src
COPY *.sln ./
COPY ./get-dockerip.sh ./get-dockerip.sh
COPY src/WebCrawler.Web/WebCrawler.Web.csproj src/WebCrawler.Web/
COPY src/WebCrawler.Shared/WebCrawler.Shared.csproj src/WebCrawler.Shared/
RUN dotnet restore
COPY . .
WORKDIR /src/src/WebCrawler.Web
RUN dotnet build -c Release -o /app

FROM build AS publish
RUN dotnet publish -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=build /src/get-dockerip.sh ./get-dockerip.sh
COPY --from=publish /app .

ENTRYPOINT ["/bin/bash","get-dockerip.sh"]

CMD ["dotnet", "WebCrawler.Web.dll"]

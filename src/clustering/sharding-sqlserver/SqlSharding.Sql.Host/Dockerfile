FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app

COPY ./bin/Release/net8.0/publish/ /app

CMD ["dotnet", "SqlSharding.Sql.Host.dll"]
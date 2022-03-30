FROM mcr.microsoft.com/mssql/server:2019-latest

COPY ./setup.sql .
COPY ./setup.sh .
COPY ./ready-check.sh .

# Grant permission on the setup script
#RUN chmod +x ./setup.sh

CMD /bin/bash -C './setup.sh';'bash'
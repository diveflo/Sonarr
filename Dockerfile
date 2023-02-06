FROM mcr.microsoft.com/dotnet/sdk:6.0-jammy
WORKDIR /source

RUN apt-get update
RUN curl -fsSL https://deb.nodesource.com/setup_19.x | bash
RUN apt-get -y install nodejs
RUN curl -sL https://dl.yarnpkg.com/debian/pubkey.gpg | gpg --dearmor | tee /usr/share/keyrings/yarnkey.gpg >/dev/null
RUN echo "deb [signed-by=/usr/share/keyrings/yarnkey.gpg] https://dl.yarnpkg.com/debian stable main" | tee /etc/apt/sources.list.d/yarn.list
RUN apt-get update && apt-get -y install yarn

COPY . .
COPY scripts/docker-entrypoint.sh /sbin/docker-entrypoint.sh

RUN chmod 755 /sbin/docker-entrypoint.sh

RUN yarn install
RUN yarn run build

RUN dotnet build src/Sonarr.sln -c release -o _output

VOLUME /config /downloads /tv
EXPOSE 8989

ENTRYPOINT ["/sbin/docker-entrypoint.sh"]
#ENTRYPOINT [ "/bin/bash", "./Sonarr", "-nobrowser", "-data=/config" ]
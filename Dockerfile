FROM mcr.microsoft.com/dotnet/runtime:6.0
ARG TARGETOS
ARG TARGETARCH

ENV LINK="https://github.com/diveflo/Sonarr/releases/latest/download/"
ENV LINK="${LINK}sonarr-hevc-${TARGETOS}-${TARGETARCH}.zip"

RUN apt-get update
RUN apt-get install -y unzip wget sqlite3

RUN mkdir -p /opt/Sonarr
RUN wget $LINK -O /opt/sonarr-hevc.zip
RUN unzip /opt/sonarr-hevc.zip -d /opt

RUN chmod -R +r /opt/Sonarr
RUN chmod 755 /opt/Sonarr/Sonarr
RUN chmod 755 /opt/Sonarr/ffprobe

COPY scripts/docker-entrypoint.sh /sbin/docker-entrypoint.sh
RUN chmod 755 /sbin/docker-entrypoint.sh

EXPOSE 8989
VOLUME /config /downloads /tv
ENTRYPOINT ["/sbin/docker-entrypoint.sh"]

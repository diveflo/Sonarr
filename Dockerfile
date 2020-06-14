FROM lsiobase/mono:LTS

# set version label
ARG BUILD_DATE
ARG VERSION
ARG SONARR_VERSION
LABEL build_version="Sonarr HEVC version:- ${VERSION} Build-date:- ${BUILD_DATE}"
LABEL maintainer="floriang89"

# set environment variables
ARG DEBIAN_FRONTEND="noninteractive"
ENV XDG_CONFIG_HOME="/config/xdg"
ENV SONARR_BRANCH="phandom-develop"

COPY scripts/docker-entrypoint.sh /sbin/docker-entrypoint.sh

RUN chmod 755 /sbin/docker-entrypoint.sh

RUN \
 echo "**** install packages ****" && \
 apt-get update && \
 apt-get install -y \
        jq wget && \
 echo "**** install sonarr ****" && \
 mkdir -p /opt/Sonarr && \
    wget https://github.com/diveflo/Sonarr/releases/latest/download/sonarr-hevc.zip -O /tmp/sonarr-hevc.zip && \
 tar xf \
	/tmp/sonarr-hevc.zip -C \
	/opt/Sonarr --strip-components=1 && \
 echo "**** cleanup ****" && \
 apt-get clean && \
 rm -rf \
	/tmp/* \
	/var/tmp/*

# ports and volumes
EXPOSE 8989
VOLUME /config /downloads /tv
ENTRYPOINT ["/sbin/docker-entrypoint.sh"]

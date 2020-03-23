#!/bin/bash

UMASK_SET=${UMASK_SET:-022}

umask "$UMASK_SET"

cd /opt/Sonarr || exit

chown -R "$PUID":"$PGID" /opt/Sonarr
chown -R "$PUID":"$PGID" /config

exec \
 mono Sonarr.exe \
 -nobrowser -debug -data=/config

#!/bin/bash

UMASK_SET=${UMASK_SET:-022}

umask "$UMASK_SET"

cd /opt/NzbDrone || exit

chown -R "$PUID":"$PGID" /opt/NzbDrone
chown -R "$PUID":"$PGID" /config

exec \
 mono NzbDrone.exe \
 -nobrowser -data=/config

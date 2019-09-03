#!/usr/bin/with-contenv bash

UMASK_SET=${UMASK_SET:-022}

umask "$UMASK_SET"

cd /opt/NzbDrone || exit

exec \
	mono NzbDrone.exe \
	-nobrowser -data=/config
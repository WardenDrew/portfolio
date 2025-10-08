#!/bin/bash

cp -vpr --update=none /default/config/* /config
cp -vpr --update=none /default/projects/* /projects
cp -vpr --update=none /default/.bashrc "/home/${USERNAME}/.bashrc"

exec /app/code-server/bin/code-server \
    --bind-addr "0.0.0.0:${PORT}" \
    --user-data-dir /config \
    --extensions-dir /app/extensions \
    --disable-telemetry \
    --auth password \
    --disable-workspace-trust \
    --ignore-last-opened
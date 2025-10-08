#!/bin/bash

if [[ -z "${PORT}" ]]; then
    export PORT=8080;
fi

if [[ -z "${USERNAME}" ]]; then
    export USERNAME="student";
fi

if [[ -z "${PASSWORD}" ]]; then
    export PASSWORD="vscoderocks";
fi

useradd -m -s /bin/bash "${USERNAME}"
echo "${PASSWORD}" | passwd "${USERNAME}" --stdin

chown -R "${USERNAME}":"${USERNAME}" /app
chown -R "${USERNAME}":"${USERNAME}" /config
chown -R "${USERNAME}":"${USERNAME}" /projects

exec su "${USERNAME}" -c '/startup/userentrypoint.sh'
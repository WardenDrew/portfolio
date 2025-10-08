#!/bin/sh
echo "Changing working directory to script file location"
cd "$(dirname $(realpath $0))"
pwd

dotnet ef migrations add --verbose --context SESAggregator.Data.AppDbContext --output-dir Data/Migrations $1 -- build-migrations --env="../../secrets/local.env"

exit 0
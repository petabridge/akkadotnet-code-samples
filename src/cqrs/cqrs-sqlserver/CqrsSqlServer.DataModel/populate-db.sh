#!/bin/bash
# Runs EF Core migrations

parent_path=$( cd "$(dirname "${BASH_SOURCE[0]}")" ; pwd -P )
cd "$parent_path"

# Update the database without building the project
dotnet ef database update --no-build
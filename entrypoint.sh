#!/bin/sh
if [ "" = "--migrate-only" ]; then
  dotnet WeddingInvite.Api.dll --migrate-only
  exit 0
fi
dotnet WeddingInvite.Api.dll

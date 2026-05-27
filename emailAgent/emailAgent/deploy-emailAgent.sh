#!/bin/bash

# do an initial build to restore dependencies
dotnet build emailAgent/emailAgent.csproj -c Release

# publiish the package
dotnet publish emailAgent/emailAgent.csproj -c Release -r linux-x64 --self-contained false -o deploy/emailAgent

# zip it Update
cd deploy
zip -r emailAgent.zip emailAgent
cd ..

echo "emailAgent has been built and packaged successfully."

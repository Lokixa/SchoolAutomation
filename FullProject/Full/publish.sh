#!/bin/bash

dotnet publish -c Release
PROJECT=$PWD
cd bin/Release/net5.0/publish/
cp $PROJECT/../../drivers/* .
./Full

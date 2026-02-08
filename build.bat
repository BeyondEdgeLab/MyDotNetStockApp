@echo off
echo Building Docker image stockapp-image...
docker build -t stockapp-image .
echo Build and tag completed.
pause
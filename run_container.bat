@echo off
echo Stopping old container if it exists...
docker stop my-stock-app 2>nul
docker rm my-stock-app 2>nul

echo Running new container...
docker run -d -p 8080:8080 -p 8081:8081 --name my-stock-app stockapp-image

if %ERRORLEVEL% EQU 0 (
    echo Container started successfully!
    echo Application will be available at http://localhost:8080
    echo Try these endpoints:
    echo   http://localhost:8080/stock/AAPL
    echo   http://localhost:8080/stock/MSFT?startDate=2024-01-01^&endDate=2024-02-07
) else (
    echo Failed to start container. Make sure the image 'stockapp-image' exists.
    echo Run build.bat first to create the image.
)
pause
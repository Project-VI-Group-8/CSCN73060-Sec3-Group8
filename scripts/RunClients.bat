@echo off
setlocal

set SERVER_IP=127.0.0.1
set SERVER_PORT=54000
set CSV_PATH=D:\Project-VI\CSCN73060-Sec3-Group8\data\katl-kefd-B737-700.txt
set DELAY_MS=0
set COUNT=20

for /L %%i in (1,1,%COUNT%) do (
    start "" /MIN D:\Project-VI\CSCN73060-Sec3-Group8\x64\Debug\Client.exe %SERVER_IP% %SERVER_PORT% %CSV_PATH% %DELAY_MS%
)

endlocal

#include <iostream>
#include "CSVReader.h"
#include "TelemetryRecord.h"

#include <windows.networking.sockets.h>


int main() {

	WSADATA wsaData;

    if ((WSAStartup(MAKEWORD(2, 2), &wsaData)) != 0) {
        return -1;
    }

	SOCKET ClientSocket = socket(AF_INET, SOCK_DGRAM, IPPROTO_UDP);
    if (ClientSocket == INVALID_SOCKET) {
        WSACleanup();
        return -1;
	}

    sockaddr_in serverAddr;
	serverAddr.sin_family = AF_INET;
	serverAddr.sin_port = htons(54000);
    serverAddr.sin_addr.s_addr = inet_addr("127.0.0.1");





    std::string testFile = "katl-kefd-B737-700.txt";

    CSVReader reader(testFile);
    TelemetryRecord record;

    int count = 0;

    // Read Next will skip the header automatically on the first pass
    while (reader.ReadNext(record) && count < 10) {
        std::cout << "Record " << (count + 1) << " -> "
            << "Time: [" << record.timestamp << "], "
            << "Fuel: [" << record.fuel_qty << "]" << std::endl;
        count++;


		// Send the record to the server
		std::string message = "Time: [" + record.timestamp + "], Fuel: [" + std::to_string(record.fuel_qty) + "]";
		int sendResult = sendto(ClientSocket, message.c_str(), message.size(), 0, (sockaddr*)&serverAddr, sizeof(serverAddr));

        if (sendResult == SOCKET_ERROR) {
            std::cout << "Error sending packet. Error: " << WSAGetLastError() << std::endl;
        }
		Sleep(1000); // Sleep for 1 second between sends to simulate real-time telemetry
    }

    if (count == 0) {
        std::cout << "\nNo records read." << std::endl;
    }
    else {
        std::cout << "\nFound 10 records." << std::endl;
    }



    closesocket(ClientSocket);
    WSACleanup();

    return 0;
}
// Server.cpp : This file contains the 'main' function. Program execution begins and ends there.
//

#include <iostream>
#include <windows.networking.sockets.h>

using namespace std;

int main()
{
    WSADATA wsaData;

	if ((WSAStartup(MAKEWORD(2, 2), &wsaData)) != 0) {
		return -1;
	}

	SOCKET ServerSocket = socket(AF_INET, SOCK_DGRAM, IPPROTO_UDP);
	if (ServerSocket == INVALID_SOCKET) {
		WSACleanup();
		return -1;
	}

	sockaddr_in serverAddr;
	serverAddr.sin_family = AF_INET;
	serverAddr.sin_port = htons(54000);
	serverAddr.sin_addr.s_addr = INADDR_ANY;

	if (bind(ServerSocket, (sockaddr*)&serverAddr, sizeof(serverAddr)) == SOCKET_ERROR) {
		closesocket(ServerSocket);
		WSACleanup();
		return -1;
	}	

	cout << "Waiting for client" << endl;

	while (1)
	{
		char rxbuffer[1024] = { };
		sockaddr_in clientAddr;
		int len = sizeof(clientAddr);

		int n = recvfrom(ServerSocket, rxbuffer, sizeof(rxbuffer), 0, (struct sockaddr*)&clientAddr, &len);

		if (n == SOCKET_ERROR)
		{
			std::cout << "Error receiving packet. Error: " << WSAGetLastError() << std::endl;
		}
		else
		{
			std::cout << "Received packet from " << inet_ntoa(clientAddr.sin_addr) << ":" << ntohs(clientAddr.sin_port) << std::endl;
			std::cout << "Data: " << rxbuffer << std::endl;

		}
	}
	

	closesocket(ServerSocket);
	WSACleanup();
	return 1;
    
}


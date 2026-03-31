// Server.cpp : This file contains the 'main' function. Program execution begins and ends there.
//

#include <iostream>
#include <windows.networking.sockets.h>
#include "ClientHandler.h"
#include <vector>

using namespace std;

int main()
{
    WSADATA wsaData;
	if ((WSAStartup(MAKEWORD(2, 2), &wsaData)) != 0) {
		return -1;
	}

	SOCKET listenerSocket = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
	if (listenerSocket == INVALID_SOCKET) {
		cout << "Error creating socket. Error: " << WSAGetLastError() << endl;
		WSACleanup();
		return -1;
	}

	sockaddr_in svrAddr;
	svrAddr.sin_family = AF_INET;
	svrAddr.sin_port = htons(54000);
	svrAddr.sin_addr.s_addr = INADDR_ANY;

	if (bind(listenerSocket, (sockaddr*)&svrAddr, sizeof(svrAddr)) == SOCKET_ERROR) {
		closesocket(listenerSocket);
		WSACleanup();
		return -1;
	}

	if (listen(listenerSocket, SOMAXCONN) == SOCKET_ERROR) {
		closesocket(listenerSocket);
		WSACleanup();
		return -1;
	}

	cout << "Server listening on port 54000" << endl;



	vector<unique_ptr<ClientHandler>> clients;

	while (true)
	{
		sockaddr_in clientAddr{};
		int addrLen = sizeof(clientAddr);

		SOCKET clientSocket = accept(listenerSocket, (sockaddr*)&clientAddr, &addrLen);
		if (clientSocket == INVALID_SOCKET) {
			cout << "Error accepting connection. Error: " << WSAGetLastError() << endl;
			continue;
		}

		cout << "Accepted Connection from " << inet_ntoa(clientAddr.sin_addr) << ":" << ntohs(clientAddr.sin_port) << endl;

		// Create connection handler
		auto handler = make_unique<ClientHandler>(clientSocket, clientAddr);
		handler->Start();
		clients.push_back(std::move(handler));
	}
	

	// Clean up handlers
	for (auto& handler : clients) {
		handler->Stop();
	}

	// Close listener socket and clean up Winsock
	closesocket(listenerSocket);
	WSACleanup();
	return 1;
    
}


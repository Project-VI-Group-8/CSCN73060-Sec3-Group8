// Server.cpp : This file contains the 'main' function. Program execution begins and ends there.
//

#include <iostream>
#include <windows.networking.sockets.h>
#include "ClientHandler.h"
#include "DataHandler.h"
#include <vector>
#include <string>
#include <atomic>

using namespace std;

const int DEFAULT_PORT = 54000;
int main(int argc, char* argv[])
{
	int port = DEFAULT_PORT;
	if (argc > 1) {
		if (atoi(argv[1]) <= 0 || atoi(argv[1]) > 65535) {
			cout << "Invalid port number. Using default port: " << DEFAULT_PORT << endl;
			port = DEFAULT_PORT;
		}
		else {
			port = atoi(argv[1]);
		}
	}

	// Initialize Winsock
    WSADATA wsaData;
	if ((WSAStartup(MAKEWORD(2, 2), &wsaData)) != 0) {
		return -1;
	}

	// Create a listening socket
	SOCKET listenerSocket = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
	if (listenerSocket == INVALID_SOCKET) {
		cout << "Error creating socket. Error: " << WSAGetLastError() << endl;
		WSACleanup();
		return -1;
	}

	// Configure the server address structure
	sockaddr_in svrAddr;
	svrAddr.sin_family = AF_INET;
	svrAddr.sin_port = htons(port);
	svrAddr.sin_addr.s_addr = INADDR_ANY;

	// Bind the socket to the specified IP and port
	if (bind(listenerSocket, (sockaddr*)&svrAddr, sizeof(svrAddr)) == SOCKET_ERROR) {
		closesocket(listenerSocket);
		WSACleanup();
		return -1;
	}

	// Start listening for incoming connections
	if (listen(listenerSocket, SOMAXCONN) == SOCKET_ERROR) {
		closesocket(listenerSocket);
		WSACleanup();
		return -1;
	}


	cout << "Server listening on port " << port << endl;

	// Start the data handler
	DataHandler dataHandler("server_data.log");
	dataHandler.Start();

	// Vector to store active client handlers
	vector<unique_ptr<ClientHandler>> clients;
	std::atomic_int nextAircraftId{ 1 };

	while (true)
	{
		// Create a socket for the incoming connection
		sockaddr_in clientAddr;
		int len = sizeof(clientAddr);

		// Accept an incoming connection
		SOCKET clientSocket = accept(listenerSocket, (sockaddr*)&clientAddr, &len);
		if (clientSocket == INVALID_SOCKET) {
			cout << "Error accepting connection. Error: " << WSAGetLastError() << endl;
			continue;
		}

		const int aircraftId = nextAircraftId.fetch_add(1);

		cout << "Accepted aircraft " << aircraftId << " from "
			<< inet_ntoa(clientAddr.sin_addr) << ":" << ntohs(clientAddr.sin_port) << endl;

		// Start a new client handler for the accepted connection
		auto handler = make_unique<ClientHandler>(clientSocket, clientAddr, aircraftId, &dataHandler);
		handler->Start();
		clients.push_back(std::move(handler));
	}
	
	// Clean up handlers
	for (auto& handler : clients) {
		handler->Stop();
	}
	
	dataHandler.Stop();

	// Close listener socket and clean up Winsock
	closesocket(listenerSocket);
	WSACleanup();
	return 1;
}

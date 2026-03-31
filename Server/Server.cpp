// Server.cpp : This file contains the 'main' function. Program execution begins and ends there.
//

#include <iostream>
#include <windows.networking.sockets.h>
#include "ClientHandler.h"
#include <vector>

using namespace std;

const int DEFAULT_PORT = 54000;
const char* DEFAULT_IP = { "127.0.0.1" };

int main(int argc, char* argv[])
{
	int port = DEFAULT_PORT;
	string ip = DEFAULT_IP;
	// Parse command line arguments for port and IP
	if (argc > 2) {

		// Validate port number
		if (atoi(argv[1]) <= 0 || atoi(argv[1]) > 65535) {
			cout << "Invalid port number. Using default port: " << DEFAULT_PORT << endl;
			port = DEFAULT_PORT;
		}
		else {
			port = atoi(argv[1]);
		}

		// Validate IP address format (basic check)
		if (inet_addr(argv[2]) == INADDR_NONE) {
			cout << "Invalid IP address format. Using default IP: " << DEFAULT_IP << endl;
			ip = DEFAULT_IP;
		}
		else {
			ip = argv[2];
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

	// Vector to store active client handlers
	vector<unique_ptr<ClientHandler>> clients;

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

		// Log the accepted connection
		// Write to file instead of screen.
		cout << "Accepted Connection from " << inet_ntoa(clientAddr.sin_addr) << ":" << ntohs(clientAddr.sin_port) << endl;

		// Start a new client handler for the accepted connection
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


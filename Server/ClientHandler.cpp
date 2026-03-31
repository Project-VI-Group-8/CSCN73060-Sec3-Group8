#include "ClientHandler.h"
#include <iostream>

ClientHandler::ClientHandler(SOCKET clientSocket, const sockaddr_in& clientAddr)
{
	_socket = clientSocket;
	_clientAddr = clientAddr;
}


ClientHandler::~ClientHandler()
{
	Stop();
}

void ClientHandler::Start()
{
	if (_running) {
		return;
	}
	_running = true;
	_thread = std::thread(&ClientHandler::Run, this);
}

void ClientHandler::Stop()
{
	if (!_running) {
		return;
	}

	shutdown(_socket, 2); // Gracefully shutdown the connection

	// Signal the thread to stop and wait for it to finish
	if (_thread.joinable()) {
		_thread.join();
	}

	_running = false;

	// Close the socket
	closesocket(_socket);
}

void ClientHandler::Run()
{
	char buffer[1024];
	while (_running) {
		int recvResult = recv(_socket, buffer, sizeof(buffer) - 1, 0);
		if (recvResult > 0) {
			buffer[recvResult] = '\0'; // Null-terminate the received data
			std::cout << "Received from " << inet_ntoa(_clientAddr.sin_addr) << ":" << ntohs(_clientAddr.sin_port) << " -> " << buffer << std::endl;
		}
		else if (recvResult == 0) {
			std::cout << "Client disconnected: " << inet_ntoa(_clientAddr.sin_addr) << ":" << ntohs(_clientAddr.sin_port) << std::endl;
			_running = false; // Client disconnected
		}
		else {
			std::cout << "Error receiving data. Error: " << WSAGetLastError() << std::endl;
			_running = false; // Error occurred
		}
	}
}

bool ClientHandler::IsRunning()
{
	return _running;
}

double ClientHandler::CalculateFuelConsumption()
{
	_fuelConsumption = _previousFuelQty - _currentFuelQty;

	return _fuelConsumption;
}
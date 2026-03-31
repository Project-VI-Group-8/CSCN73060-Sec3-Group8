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

double ClientHandler::CalculateFuelConsumption(double fuelQty)
{
	if (_startFuelQty == 0.0) {
		_startFuelQty = fuelQty; // Set the starting fuel quantity on the first received data
	}
	else {
		_currentFuelQty = fuelQty; // Update the current fuel quantity with the latest received data
		_finalFuelQty = _currentFuelQty; // Update the final fuel quantity for consumption calculation
		double consumed = _startFuelQty - _finalFuelQty; // Calculate consumed fuel
		_totalFuelConsumed += consumed; // Update total fuel consumed
		std::cout << "Calculated Fuel Consumption: " << consumed << " (Total Consumed: " << _totalFuelConsumed << ")" << std::endl;
	}
	return _totalFuelConsumed; // Return total fuel consumed for this client
}
#include "ClientHandler.h"
#include <iostream>

ClientHandler::ClientHandler(SOCKET clientSocket, const sockaddr_in& clientAddr, int aircraftId, DataHandler* dataHandler)
{
	_socket = clientSocket;
	_clientAddr = clientAddr;
	_aircraftId = aircraftId;
	_dataHandler = dataHandler;
}

ClientHandler::~ClientHandler()
{
	Stop();
}

void ClientHandler::Start()
{
	// Check if the thread is already running
	if (_running) 
	{
		return;
	}

	_running = true;
	_thread = std::thread(&ClientHandler::Run, this);
}

void ClientHandler::Stop()
{
	// Check if the thread is already stopped
	if (!_running) 
	{
		return;
	}

	// Gracefully shutdown the connection
	shutdown(_socket, 2); 

	// Signal the thread to stop and wait for it to finish
	if (_thread.joinable()) 
	{
		_thread.join();
	}

	_running = false;

	// Close the socket
	closesocket(_socket);
}

void ClientHandler::Run()
{
	const std::string idMessage = std::to_string(_aircraftId);
	if (send(_socket, idMessage.c_str(), static_cast<int>(idMessage.size()), 0) == SOCKET_ERROR) {
		std::string msg = "Error sending aircraft ID " + std::to_string(_aircraftId)
			+ ". Error: " + std::to_string(WSAGetLastError());
		if (_dataHandler) _dataHandler->AddData(msg);
		else std::cout << msg << std::endl;
		_running = false;
		return;
	}

	char buffer[1024];
	while (_running) {
		int recvResult = recv(_socket, buffer, sizeof(buffer) - 1, 0);
		if (recvResult > 0) {
			buffer[recvResult] = '\0';
			std::string msg = "Received from aircraft " + std::to_string(_aircraftId) + " "
				+ std::string(inet_ntoa(_clientAddr.sin_addr)) + ":" + std::to_string(ntohs(_clientAddr.sin_port))
				+ " -> " + buffer;
			if (_dataHandler) _dataHandler->AddData(msg);
			else std::cout << msg << std::endl;
		}
		else if (recvResult == 0) {
			std::string msg = "Aircraft " + std::to_string(_aircraftId) + " disconnected: "
				+ std::string(inet_ntoa(_clientAddr.sin_addr)) + ":" + std::to_string(ntohs(_clientAddr.sin_port));
			if (_dataHandler) _dataHandler->AddData(msg);
			else std::cout << msg << std::endl;
			_running = false; // Client disconnected
		}
		else {
			std::string msg = "Error receiving data. Error: " + std::to_string(WSAGetLastError());
			if (_dataHandler) _dataHandler->AddData(msg);
			else std::cout << msg << std::endl;
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

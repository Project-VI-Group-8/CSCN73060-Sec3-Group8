#include "ClientHandler.h"
#include <iostream>

/// @brief Constructs a ClientHandler for a given client socket and address, and a pointer to the data handler for logging received messages.
/// @param clientSocket incoming client socket to handle communication with the client
/// @param clientAddr address information for the client, used for logging purposes
/// @param dataHandler data handler pointer for logging received messages
ClientHandler::ClientHandler(SOCKET clientSocket, const sockaddr_in& clientAddr, DataHandler* dataHandler)
{
	_socket = clientSocket;
	_clientAddr = clientAddr;
	_dataHandler = dataHandler;
}

/// @brief Destructor for ClientHandler. Stops the client handler thread and cleans up resources.
ClientHandler::~ClientHandler()
{
	Stop();
}

/// @brief Starts client handler thread 
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

/// @brief Stops client handler thread and cleans up resources.
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

/// @brief Main working loop for client thread. Receives data from the client socket and processes telemetry data.
void ClientHandler::Run()
{
	char buffer[1024];
	while (_running) {
		int recvResult = recv(_socket, buffer, sizeof(buffer) - 1, 0);
		if (recvResult > 0) {
			buffer[recvResult] = '\0';
			std::string msg = "Received from " + std::string(inet_ntoa(_clientAddr.sin_addr)) + ":" + std::to_string(ntohs(_clientAddr.sin_port)) + " -> " + buffer;
			if (_dataHandler) _dataHandler->AddData(msg);
			else std::cout << msg << std::endl;
		}
		else if (recvResult == 0) {
			std::string msg = "Client disconnected: " + std::string(inet_ntoa(_clientAddr.sin_addr)) + ":" + std::to_string(ntohs(_clientAddr.sin_port));
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

/// @brief Check if the client handler thread is currently running.
/// @return True/False indicating if the client handler thread is running.
bool ClientHandler::IsRunning()
{
	return _running;
}

/// @brief Calculates the fuel consumption based on previous and current fuel quantities
/// @return The calculated fuel consumption
double ClientHandler::CalculateFuelConsumption()
{
	_fuelConsumption = _previousFuelQty - _currentFuelQty;

	return _fuelConsumption;
}
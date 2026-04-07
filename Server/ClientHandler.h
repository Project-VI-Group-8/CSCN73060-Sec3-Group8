#pragma once
#include <windows.networking.sockets.h>
#include <thread>
#include <atomic>
#include "DataHandler.h"

class ClientHandler
{
private:
	DataHandler* _dataHandler;						// Pointer to data handler for logging received messages
	SOCKET				_socket;					// Client socket for communication
	sockaddr_in			_clientAddr;				// Client address information
	std::thread			_thread;					// Thread for handling client communication
	std::atomic_bool	_running{ false };			// If client is running

	int					_messageCount{ 0 };			// Count of messages received
	double				_startFuelQty{ 0.0 };		// Starting fuel quantity 
	double 				_previousFuelQty{ 0.0 };	// Total fuel consumed based on received data		
	double				_currentFuelQty{ 0.0 };		// Current based on the latest received data
	double				_finalFuelQty{ 0.0 };		// Final fuel quantity received

	double				_fuelConsumption{ 0.0 };	// The current fuel consumption

	double	CalculateFuelConsumption();				// Calculate fuel consumption
	void	Run();									// Main loop
public:
	ClientHandler(SOCKET clientSocket, const sockaddr_in& clientAddr, DataHandler* dataHandler);	// Constructor
	~ClientHandler();																				// Destructor

	void Start();									// Start the client handler thread
	void Stop();									// Stop the client handler thread and clean up resources
	bool IsRunning();								// Check if the client handler is currently running
};


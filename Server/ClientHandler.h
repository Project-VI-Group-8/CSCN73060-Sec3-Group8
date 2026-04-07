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

	/// @brief The current fuel consumption.
	double				_fuelConsumption{ 0.0 };	// The current fuel consumption

	/// @brief Calculates the fuel consumption based on previous and current fuel quantities
	/// @return The calculated fuel consumption
	double	CalculateFuelConsumption();				// Calculate fuel consumption

	/// @brief Main working loop for client thread. Receives data from the client socket and processes telemetry data.
	void	Run();									// Main loop
public:

	/// @brief Constructs a ClientHandler for a given client socket and address, and a pointer to the data handler for logging received messages.
	/// @param clientSocket incoming client socket to handle communication with the client
	/// @param clientAddr address information for the client, used for logging purposes
	/// @param dataHandler data handler pointer for logging received messages
	ClientHandler(SOCKET clientSocket, const sockaddr_in& clientAddr, DataHandler* dataHandler);	// Constructor

	/// @brief Destructor for ClientHandler. Stops the client handler thread and cleans up resources.
	~ClientHandler();																				// Destructor

	/// @brief Starts client handler thread 
	void Start();									// Start the client handler thread

	/// @brief Stops client handler thread and cleans up resources.
	void Stop();									// Stop the client handler thread and clean up resources

	/// @brief Check if the client handler thread is currently running.
	/// @return True/False indicating if the client handler thread is running.
	bool IsRunning();								// Check if the client handler is currently running
};


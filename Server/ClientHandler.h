#pragma once
#include <WinSock2.h>
#include <string>
#include <thread>
#include <atomic>
#include <ctime>
#include "DataHandler.h"

class ClientHandler
{
private:
	DataHandler* _dataHandler;						// Pointer to data handler for logging received messages
	SOCKET				_socket;					// Client socket for communication
	sockaddr_in			_clientAddr;				// Client address information
	int					_aircraftId;				// Unique aircraft ID assigned by the server
	std::thread			_thread;					// Thread for handling client communication
	std::atomic_bool	_running{ false };			// If client is running

	int					_packetCount{ 0 };			// Count of parsed telemetry packets received
	double 				_previousFuelQty{ 0.0 };	// Fuel quantity from the prior telemetry packet
	std::time_t			_previousTimestamp{ 0 };	// Timestamp from the prior telemetry packet
	bool				_hasPreviousSample{ false };	// Whether a prior telemetry sample is available
	double				_runningAverage{ 0.0 };		// Running average fuel consumption in gallons per second
	int					_averageSampleCount{ 0 };	// Number of instantaneous rates included in the running average
	std::string			_lastTimestamp;				// Timestamp from the last valid packet

	/// @brief Main working loop for client thread. Receives data from the client socket and processes telemetry data.
	void	Run();									// Main loop
public:

	/// @brief Constructs a ClientHandler for a given client socket and address, and a pointer to the data handler for logging received messages.
	/// @param clientSocket incoming client socket to handle communication with the client
	/// @param clientAddr address information for the client, used for logging purposes
	/// @param aircraftId unique aircraft ID assigned by the server for this connection
	/// @param dataHandler data handler pointer for logging received messages
	ClientHandler(SOCKET clientSocket, const sockaddr_in& clientAddr, int aircraftId, DataHandler* dataHandler);	// Constructor

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

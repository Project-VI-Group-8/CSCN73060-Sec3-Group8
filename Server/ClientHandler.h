#pragma once
#include <windows.networking.sockets.h>
#include <thread>
#include <atomic>

class ClientHandler
{
private:
	SOCKET				_socket;			// Client socket for communication
	sockaddr_in			_clientAddr;		// Client address information
	std::thread			_thread;			// Thread for handling client communication
	std::atomic_bool	_running{ false };	// Flag to indicate if the client handler is running

	double				_startFuelQty{ 0.0 };		// Starting fuel quantity for this client, initialized to 0.0
	double				_currentFuelQty{ 0.0 };		// Current fuel quantity based on the latest received data, initialized to 0.0
	double				_finalFuelQty{ 0.0 };		// Final fuel quantity received, used for calculating consumption, initialized to 0.0
	double				_totalFuelConsumed{ 0.0 };	// Example variable to track total fuel consumed for this client

	double CalculateFuelConsumption(double fuelQty); // Example method to calculate fuel consumption based on received data
	void Run();			// Main loop for handling client communication
public:
	ClientHandler(SOCKET clientSocket, const sockaddr_in& clientAddr);	// Constructor
	~ClientHandler();													// Destructor

	void Start();		// Start the client handler thread
	void Stop();		// Stop the client handler thread and clean up resources
	bool IsRunning();	// Check if the client handler is currently running
};


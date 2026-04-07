#pragma once
#include <string>
#include <queue>
#include <mutex>
#include <condition_variable>
#include <thread>
#include <atomic>
#include <fstream>
#include <iostream>

class DataHandler
{
private:
	std::queue<std::string> _queue;				// Shared queue for Client Writes
	std::mutex				_mutex;				// Mutex to synchronize writes to queue
	std::condition_variable _cv;				// Conditional variable to signal when new data is in queue
	std::thread				_thread;			// The data handler thread
	std::atomic<bool>		_running{ false };	// The running state
	std::ofstream			_file;				// The log file to write to

	/// @brief Main loop for data handler thread.
	//  Waits for data to be added and writes to a file
	void Run();									// Worker loop

	/// @brief Writes all data from the queue to the file.
	void WriteData();							// Write data from queue to file

public:
	/// @brief Constructs a DataHandler and opens the specified file in append mode.
	/// @param filename Path to the log file.
	DataHandler(const std::string& filename);	// Constructor

	/// @brief destructor for DataHandler. Stops the thread and closes the file if it is open.
	~DataHandler();								// Destructor 

	/// @brief Starts the data handler thread if it is not already running. The thread will run the Run() method.
	void Start();								// Start the data handler thread

	/// @brief Stops the data handler thread if it is running. Signals the thread to stop, waits for it to finish, and ensures that all remaining data in the queue is written to the file before exiting.
	void Stop();								// Stop the data handler thread

	/// @brief Adds data to the queue for processing
	/// @param data string data to add to the queue.
	void AddData(const std::string& data);		// Add data to the queue for processing

	/// @brief Checks if the data handler thread is currently running.
	/// @return True/False indicating if the data handler thread is running.
	bool IsRunning();							// Check if the data handler is currently running
};

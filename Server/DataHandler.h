#pragma once
#include <string>
#include <queue>
#include <mutex>
#include <condition_variable>
#include <thread>
#include <atomic>
#include <fstream>

class DataHandler
{
private:
	std::queue<std::string> _queue;				// Shared queue for Client Writes
	std::mutex				_mutex;				// Mutex to synchronize writes to queue
	std::condition_variable _cv;				// Conditional variable to signal when new data is in queue
	std::thread				_thread;			// The data handler thread
	std::atomic<bool>		_running{ false };	// The running state
	std::ofstream			_file;				// The log file to write to

	void Run();									// Worker loop
	void WriteData();							// Write data from queue to file

public:
	DataHandler(const std::string& filename);	// Constructor
	~DataHandler();								// Destructor 

	void Start();								// Start the data handler thread
	void Stop();								// Stop the data handler thread
	void AddData(const std::string& data);		// Add data to the queue for processing
};

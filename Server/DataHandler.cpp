#include "DataHandler.h"

/// @brief Constructs a DataHandler and opens the specified file in append mode.
/// @param filename Path to the log file.
DataHandler::DataHandler(const std::string& filename)
{
	// Open the file
	_file.open(filename, std::ios::app);

	// Check if the file was opened successfully
	if (!_file.is_open()) 
	{
		std::cerr << "Failed to open file: " << filename << std::endl;
	}
}

/// @brief destructor for DataHandler. Stops the thread and closes the file if it is open.
DataHandler::~DataHandler()
{
	// Stop the thread
	Stop();
	if (_file.is_open()) // Try to close the file
	{
		_file.close();
	}
}

/// @brief Starts the data handler thread if it is not already running. The thread will run the Run() method.
void DataHandler::Start()
{
	// Check if the thread is already running
	if (_running)
	{
		return;
	}

	// Spawn the thread to process the data
	_running = true;
	_thread = std::thread(&DataHandler::Run, this);
}

/// @brief Stops the data handler thread if it is running. Signals the thread to stop, waits for it to finish, and ensures that all remaining data in the queue is written to the file before exiting.
void DataHandler::Stop()
{
	// Check if the thread is already stopped
	if (!_running) 
	{
		return;
	}

	// Signal thread
	_running = false;
	_cv.notify_one();

	// Wait for the thread to finish
	if (_thread.joinable())
	{
		_thread.join();
	}
}

/// @brief Adds data to the queue for processing
/// @param data string data to add to the queue.
void DataHandler::AddData(const std::string& data)
{
	// Lock the mutex before pushing to queue
	std::unique_lock<std::mutex> lock(_mutex);
	_queue.push(data);
	lock.unlock();

	// Signal the write
	_cv.notify_one();
}

/// @brief Writes all data from the queue to the file.
void DataHandler::WriteData()
{
	// Grab the lock
	std::unique_lock<std::mutex> lock(_mutex);

	// Write data from queue to file
	while (!_queue.empty())
	{

		if (_file.is_open())
		{
			_file << _queue.front() << std::endl;
		}

		_queue.pop();
	}

}

/// @brief Main loop for data handler thread.
//  Waits for data to be added and writes to a file
void DataHandler::Run()
{
	while (_running) 
	{
		// Wait for the lock
		std::unique_lock<std::mutex> lock(_mutex);
		while (_queue.empty() && _running)
		{
			// The add data will notify the lock of data
			_cv.wait(lock);
		}

		lock.unlock();

		WriteData();
	}

	// Make sure the queue is empty
	WriteData();
}

/// @brief Checks if the data handler thread is currently running.
/// @return True/False indicating if the data handler thread is running.
bool DataHandler::IsRunning()
{
	return _running;
}
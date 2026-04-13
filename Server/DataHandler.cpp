#include "DataHandler.h"

DataHandler::DataHandler(const std::string& filename)
{
	// Start each server run with a fresh CSV log so the results from the
	// current execution are not mixed with stale output from prior runs.
	_file.open(filename, std::ios::trunc);

	// Check if the file was opened successfully
	if (!_file.is_open()) 
	{
		std::cerr << "Failed to open file: " << filename << std::endl;
	}
}

DataHandler::~DataHandler()
{
	// Stop the thread
	Stop();
	if (_file.is_open()) // Try to close the file
	{
		_file.close();
	}
}

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

void DataHandler::AddData(const std::string& data)
{
	// Lock the mutex before pushing to queue
	std::unique_lock<std::mutex> lock(_mutex);
	_queue.push(data);
	lock.unlock();

	// Signal the write
	_cv.notify_one();
}

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

bool DataHandler::IsRunning()
{
	return _running;
}

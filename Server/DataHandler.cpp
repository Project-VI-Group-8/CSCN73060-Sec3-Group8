#include "DataHandler.h"
#include <iostream>

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

	std::unique_lock<std::mutex> lock(_mutex);
	_queue.push(data);
	lock.unlock();

	_cv.notify_one();
}

void DataHandler::Run()
{
	while (_running) 
	{
		std::unique_lock<std::mutex> lock(_mutex);
		_cv.wait(lock, [this]() { return !_queue.empty() || !_running; });

		while (!_queue.empty()) 
		{
			std::string data = _queue.front();
			_queue.pop();
			lock.unlock();

			if (_file.is_open()) 
			{
				_file << data << std::endl;
			}

			lock.lock();
		}
	}
}

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
	std::queue<std::string> _queue;
	std::mutex _mutex;
	std::condition_variable _cv;
	std::thread _thread;
	std::atomic<bool> _running{ false };
	std::ofstream _file;

	void Run();

public:
	DataHandler(const std::string& filename);
	~DataHandler();

	void Start();
	void Stop();
	void AddData(const std::string& data);
};

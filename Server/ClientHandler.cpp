#include "ClientHandler.h"

#include <iostream>
#include <sstream>
#include <iomanip>
#include <optional>
#include <mstcpip.h>

namespace {
struct ParsedTelemetryPacket {
	int aircraftId = 0;
	std::string timestamp;
	double fuelQty = 0.0;
};

std::optional<std::time_t> ParseTimestamp(const std::string& timestamp)
{
	// Project telemetry files use a flexible D_M_YYYY HH:MM:SS shape, so parse
	// the date/time components manually instead of relying on a fixed-width format.
	const auto spacePos = timestamp.find(' ');
	if (spacePos == std::string::npos) {
		return std::nullopt;
	}

	const std::string datePart = timestamp.substr(0, spacePos);
	const std::string timePart = timestamp.substr(spacePos + 1);

	std::stringstream dateStream(datePart);
	std::stringstream timeStream(timePart);
	std::string dayStr;
	std::string monthStr;
	std::string yearStr;
	std::string hourStr;
	std::string minuteStr;
	std::string secondStr;

	if (!std::getline(dateStream, dayStr, '_') || !std::getline(dateStream, monthStr, '_') || !std::getline(dateStream, yearStr)) {
		return std::nullopt;
	}
	if (!std::getline(timeStream, hourStr, ':') || !std::getline(timeStream, minuteStr, ':') || !std::getline(timeStream, secondStr)) {
		return std::nullopt;
	}

	try {
		std::tm tm{};
		tm.tm_mday = std::stoi(dayStr);
		tm.tm_mon = std::stoi(monthStr) - 1;
		tm.tm_year = std::stoi(yearStr) - 1900;
		tm.tm_hour = std::stoi(hourStr);
		tm.tm_min = std::stoi(minuteStr);
		tm.tm_sec = std::stoi(secondStr);
		tm.tm_isdst = -1;

		const std::time_t parsed = std::mktime(&tm);
		if (parsed == -1) {
			return std::nullopt;
		}

		return parsed;
	}
	catch (const std::exception&) {
		return std::nullopt;
	}
}

std::optional<ParsedTelemetryPacket> ParsePacket(const std::string& packet)
{
	std::stringstream ss(packet);
	std::string idStr;
	std::string timestampStr;
	std::string fuelStr;

	if (!std::getline(ss, idStr, '|') || !std::getline(ss, timestampStr, '|') || !std::getline(ss, fuelStr)) {
		return std::nullopt;
	}

	try {
		ParsedTelemetryPacket parsed;
		parsed.aircraftId = std::stoi(idStr);
		parsed.timestamp = timestampStr;
		parsed.fuelQty = std::stod(fuelStr);
		return parsed;
	}
	catch (const std::exception&) {
		return std::nullopt;
	}
}
}

ClientHandler::ClientHandler(SOCKET clientSocket, const sockaddr_in& clientAddr, int aircraftId, DataHandler* dataHandler)
{
	_socket = clientSocket;
	_clientAddr = clientAddr;
	_aircraftId = aircraftId;
	_dataHandler = dataHandler;
}

ClientHandler::~ClientHandler()
{
	Stop();
}

void ClientHandler::Start()
{
	// Check if the thread is already running
	if (_running) 
	{
		return;
	}

	_running = true;
	_thread = std::thread(&ClientHandler::Run, this);
}

void ClientHandler::Stop()
{
	// Check if the thread is already stopped
	if (!_running) 
	{
		return;
	}

	// Gracefully shutdown the connection
	shutdown(_socket, 2); 

	// Signal the thread to stop and wait for it to finish
	if (_thread.joinable()) 
	{
		_thread.join();
	}

	_running = false;

	// Close the socket
	closesocket(_socket);
}

void ClientHandler::Run()
{
	const std::string idMessage = std::to_string(_aircraftId);
	if (send(_socket, idMessage.c_str(), static_cast<int>(idMessage.size()), 0) == SOCKET_ERROR) {
		std::string msg = "Error sending aircraft ID " + std::to_string(_aircraftId)
			+ ". Error: " + std::to_string(WSAGetLastError());
		if (_dataHandler) _dataHandler->AddData(msg);
		else std::cout << msg << std::endl;
		_running = false;
		return;
	}

	BOOL keepAlive = TRUE;
	if (setsockopt(_socket, SOL_SOCKET, SO_KEEPALIVE, reinterpret_cast<const char*>(&keepAlive), sizeof(keepAlive)) == SOCKET_ERROR) {
		std::cerr << "Failed to enable TCP keepalive. Error: " << WSAGetLastError() << std::endl;
	}

	tcp_keepalive keepAliveSettings{};
	keepAliveSettings.onoff = 1;
	keepAliveSettings.keepalivetime = 5000;
	keepAliveSettings.keepaliveinterval = 1000;
	DWORD bytesReturned = 0;

	if (WSAIoctl(_socket,
		SIO_KEEPALIVE_VALS,
		&keepAliveSettings,
		sizeof(keepAliveSettings),
		nullptr,
		0,
		&bytesReturned,
		nullptr,
		nullptr) == SOCKET_ERROR) {
		std::cerr << "Failed to configure TCP keepalive timings. Error: " << WSAGetLastError() << std::endl;
	}

	char buffer[1024];
	std::string recvBuffer;
	while (_running) {
		int recvResult = recv(_socket, buffer, sizeof(buffer) - 1, 0);
		if (recvResult > 0) {
			// TCP is a byte stream, so one recv() can contain partial packets or
			// multiple newline-delimited packets. Accumulate until a full line exists.
			recvBuffer.append(buffer, buffer + recvResult);

			std::size_t newlinePos = std::string::npos;
			while ((newlinePos = recvBuffer.find('\n')) != std::string::npos) {
				std::string packet = recvBuffer.substr(0, newlinePos);
				recvBuffer.erase(0, newlinePos + 1);

				if (!packet.empty() && packet.back() == '\r') {
					packet.pop_back();
				}
				if (packet.empty()) {
					continue;
				}

				const auto parsedPacket = ParsePacket(packet);
				if (!parsedPacket.has_value()) {
					std::cerr << "Malformed packet from aircraft " << _aircraftId
						<< ": " << packet << std::endl;
					continue;
				}
				if (parsedPacket->aircraftId != _aircraftId) {
					std::cerr << "Aircraft ID mismatch. Expected " << _aircraftId
						<< " but received " << parsedPacket->aircraftId << std::endl;
					continue;
				}
				if (parsedPacket->fuelQty < 0.0) {
					std::cerr << "Negative fuel quantity from aircraft " << _aircraftId << std::endl;
					continue;
				}

				const auto parsedTimestamp = ParseTimestamp(parsedPacket->timestamp);
				if (!parsedTimestamp.has_value()) {
					std::cerr << "Invalid timestamp from aircraft " << _aircraftId
						<< ": " << parsedPacket->timestamp << std::endl;
					continue;
				}

				++_packetCount;
				_lastTimestamp = parsedPacket->timestamp;

				if (_hasPreviousSample) {
					const double deltaFuel = _previousFuelQty - parsedPacket->fuelQty;
					const double deltaSeconds = std::difftime(*parsedTimestamp, _previousTimestamp);
					const double currentRate = (deltaSeconds > 0.0) ? deltaFuel / deltaSeconds : 0.0;

					// Maintain the average online so the handler does not need to keep
					// every instantaneous rate for the full duration of the flight.
					++_averageSampleCount;
					_runningAverage += (currentRate - _runningAverage) / static_cast<double>(_averageSampleCount);
				}

				_previousFuelQty = parsedPacket->fuelQty;
				_previousTimestamp = *parsedTimestamp;
				_hasPreviousSample = true;
			}
		}
		else if (recvResult == 0) {
			if (_packetCount > 0 && _dataHandler) {
				std::ostringstream flightRecord;
				flightRecord << _aircraftId << ','
					<< std::fixed << std::setprecision(6) << _runningAverage << ','
					<< _lastTimestamp << ','
					<< _packetCount;
				_dataHandler->AddData(flightRecord.str());
			}

			std::cout << "Aircraft " << _aircraftId << " disconnected after "
				<< _packetCount << " packets. Final average fuel consumption: "
				<< std::fixed << std::setprecision(6) << _runningAverage << " gal/s" << std::endl;
			_running = false; // Client disconnected
		}
		else {
			std::cerr << "Error receiving data from aircraft " << _aircraftId
				<< ". Error: " << WSAGetLastError() << std::endl;
			_running = false; // Error occurred
		}
	}
}

bool ClientHandler::IsRunning()
{
	return _running;
}

#pragma once
#include <string>
#include <WinSock2.h>
#include "CSVReader.h"
#include "PacketBuilder.h"

/**
 * @class TCPClient
 * @brief Manages the TCP telemetry client lifecycle.
 *
 * This class owns the client socket, CSV reader, and packet builder used to
 * stream telemetry records to the server.
 */
class TCPClient {
public:
	/**
	 * @brief Constructs a TCP client with its connection and telemetry settings.
	 * @param serverIP IPv4 address of the server.
	 * @param port TCP port used by the server.
	 * @param csvPath Path to the telemetry CSV file.
	 * @param delayMs Delay between packet sends in milliseconds.
	 */
	TCPClient(const std::string& serverIP, int port,
		const std::string& csvPath, int delayMs);

	/**
	 * @brief Initializes Winsock, creates the socket, and connects to the server.
	 */
	void Connect();

	/**
	 * @brief Runs the telemetry transmission loop.
	 */
	void Run();

	/**
	 * @brief Gracefully closes the client connection and releases socket resources.
	 */
	void Disconnect();

private:
	std::string		_serverIP;					/**< IPv4 address of the destination server. */
	int				_port;						/**< TCP port used for the server connection. */
	int				_delayMs;					/**< Inter-packet delay used to simulate streaming telemetry. */
	int				_aircraftId = -1;			/**< Unique aircraft ID assigned by the server. */
	SOCKET			_sock		= INVALID_SOCKET;	/**< Connected client socket. */
	CSVReader		_reader;					/**< Reads telemetry records from the configured CSV file. */
	PacketBuilder	_builder;					/**< Formats telemetry records into protocol packets. */
};

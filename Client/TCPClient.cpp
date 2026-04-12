#include "TCPClient.h"

#include <iostream>
#include <stdexcept>
#include <string>
#include <WS2tcpip.h>

TCPClient::TCPClient(const std::string& serverIP, int port,
	const std::string& csvPath, int delayMs)
	: _serverIP(serverIP),
	  _port(port),
	  _delayMs(delayMs),
	  _reader(csvPath)
{
}

void TCPClient::Connect()
{
	WSADATA wsaData;
	if (WSAStartup(MAKEWORD(2, 2), &wsaData) != 0) {
		throw std::runtime_error("WSAStartup failed with error " + std::to_string(WSAGetLastError()));
	}

	_sock = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
	if (_sock == INVALID_SOCKET) {
		const int error = WSAGetLastError();
		WSACleanup();
		throw std::runtime_error("socket failed with error " + std::to_string(error));
	}

	sockaddr_in serverAddr{};
	serverAddr.sin_family = AF_INET;
	serverAddr.sin_port = htons(static_cast<u_short>(_port));

	if (InetPtonA(AF_INET, _serverIP.c_str(), &serverAddr.sin_addr) != 1) {
		closesocket(_sock);
		_sock = INVALID_SOCKET;
		WSACleanup();
		throw std::runtime_error("Invalid server IP address: " + _serverIP);
	}

	if (connect(_sock, reinterpret_cast<sockaddr*>(&serverAddr), sizeof(serverAddr)) == SOCKET_ERROR) {
		const int error = WSAGetLastError();
		closesocket(_sock);
		_sock = INVALID_SOCKET;
		WSACleanup();
		throw std::runtime_error("connect failed with error " + std::to_string(error));
	}

	char idBuffer[32] = {};
	const int recvResult = recv(_sock, idBuffer, sizeof(idBuffer) - 1, 0);
	if (recvResult <= 0) {
		const int error = recvResult == 0 ? 0 : WSAGetLastError();
		closesocket(_sock);
		_sock = INVALID_SOCKET;
		WSACleanup();
		throw std::runtime_error("Failed to receive aircraft ID. Error " + std::to_string(error));
	}

	idBuffer[recvResult] = '\0';

	try {
		_aircraftId = std::stoi(idBuffer);
	}
	catch (const std::exception&) {
		closesocket(_sock);
		_sock = INVALID_SOCKET;
		WSACleanup();
		throw std::runtime_error("Received invalid aircraft ID from server: " + std::string(idBuffer));
	}

	if (_aircraftId <= 0) {
		closesocket(_sock);
		_sock = INVALID_SOCKET;
		WSACleanup();
		throw std::runtime_error("Received non-positive aircraft ID from server: " + std::to_string(_aircraftId));
	}

	std::cout << "Connected to server as aircraft " << _aircraftId << std::endl;
}

void TCPClient::Run()
{
	if (_sock == INVALID_SOCKET) {
		throw std::runtime_error("TCPClient::Run called before a successful connection");
	}

	if (_aircraftId <= 0) {
		throw std::runtime_error("TCPClient::Run called without a valid aircraft ID");
	}

	if (!_reader.IsOpen()) {
		throw std::runtime_error("Telemetry CSV file is not open");
	}

	TelemetryRecord record;
	int sentCount = 0;

	while (_reader.ReadNext(record)) {
		const std::string packet = PacketBuilder::Build(_aircraftId, record);
		int totalSent = 0;
		while (totalSent < static_cast<int>(packet.size())) {
			const int sendResult = send(
				_sock,
				packet.c_str() + totalSent,
				static_cast<int>(packet.size()) - totalSent,
				0);

			if (sendResult == SOCKET_ERROR) {
				throw std::runtime_error("send failed with error " + std::to_string(WSAGetLastError()));
			}

			totalSent += sendResult;
		}

		++sentCount;
		std::cout << "Sent packet " << sentCount << " for aircraft " << _aircraftId << std::endl;

		if (_delayMs > 0) {
			Sleep(_delayMs);
		}
	}

	std::cout << "Completed telemetry transmission for aircraft " << _aircraftId
		<< " after " << sentCount << " packets" << std::endl;
}

void TCPClient::Disconnect()
{
	if (_sock != INVALID_SOCKET) {
		shutdown(_sock, SD_SEND);
		closesocket(_sock);
		_sock = INVALID_SOCKET;
	}

	WSACleanup();
}

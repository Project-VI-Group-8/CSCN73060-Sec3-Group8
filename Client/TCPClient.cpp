#include "TCPClient.h"

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
}

void TCPClient::Run()
{
	throw std::runtime_error("TCPClient::Run is not implemented yet");
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

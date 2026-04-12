#include <iostream>
#include <stdexcept>
#include <string>

#include "TCPClient.h"

namespace {
constexpr int DEFAULT_PORT = 54000;
constexpr int DEFAULT_DELAY_MS = 1000;
constexpr char DEFAULT_SERVER_IP[] = "127.0.0.1";
constexpr char DEFAULT_CSV_PATH[] = "katl-kefd-B737-700.txt";
}

int main(int argc, char* argv[]) {
	std::string serverIP = DEFAULT_SERVER_IP;
	int port = DEFAULT_PORT;
	std::string csvPath = DEFAULT_CSV_PATH;
	int delayMs = DEFAULT_DELAY_MS;

	if (argc > 1) {
		serverIP = argv[1];
	}
	if (argc > 2) {
		port = std::stoi(argv[2]);
	}
	if (argc > 3) {
		csvPath = argv[3];
	}
	if (argc > 4) {
		delayMs = std::stoi(argv[4]);
	}

	try {
		TCPClient client(serverIP, port, csvPath, delayMs);
		client.Connect();
		client.Run();
		client.Disconnect();
		return 0;
	}
	catch (const std::exception& ex) {
		std::cerr << "Client error: " << ex.what() << std::endl;
		return -1;
	}
}

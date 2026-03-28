#include "CSVReader.h"
#include <sstream>
#include <iostream>
#include <algorithm>

/**
 * @brief Constructs the CSVReader and attempts to open the file stream.
 * Logs an error to stderr if the file cannot be opened.
 */
CSVReader::CSVReader(const std::string& path) : _filePath(path), _headerSkipped(false) {
    _fileStream.open(_filePath);
    if (!_fileStream.is_open()) {
        std::cerr << "Error: Could not open telemetry file at " << _filePath << std::endl;
    }
}

/**
 * @brief Reads and discards the first line of the file (the CSV header).
 */
void CSVReader::SkipHeader() {
    std::string line;
    if (std::getline(_fileStream, line)) {
        _headerSkipped = true;
    }
}

/**
 * @brief Parses a raw CSV line, extracts the timestamp and fuel quantity,
 * and handles whitespace trimming and type conversion.
 */
bool CSVReader::ParseLine(const std::string& line, TelemetryRecord& outRecord) {
    std::stringstream ss(line);
    std::string timestampStr, fuelStr; // Removed the unused 'extra' variable

    if (std::getline(ss, timestampStr, ',') && std::getline(ss, fuelStr)) {
        // Trim trailing whitespace/carriage returns from timestamp
        timestampStr.erase(timestampStr.find_last_not_of(" \n\r\t") + 1);

        // Trim leading and trailing whitespace/carriage returns from fuel quantity
        fuelStr.erase(0, fuelStr.find_first_not_of(" \n\r\t"));
        fuelStr.erase(fuelStr.find_last_not_of(" \n\r\t") + 1);

        if (timestampStr.empty() || fuelStr.empty()) {
            std::cerr << "Warning: malformed line skipped (missing fields): " << line << std::endl;
            return false;
        }

        try {
            outRecord.timestamp = timestampStr;
            outRecord.fuel_qty = std::stod(fuelStr);
            return true;
        }
        catch (const std::exception& e) {
            std::cerr << "Warning: Failed to parse fuel quantity on line: " << line << std::endl;
            return false;
        }
    }

    std::cerr << "Warning: Malformed line skipped (missing comma delimiter): " << line << std::endl;
    return false;
}

/**
 * @brief Iterates through the open file stream until a valid line is parsed or EOF is reached.
 */
bool CSVReader::ReadNext(TelemetryRecord& outRecord) {
    if (!_fileStream.is_open()) return false;

    if (!_headerSkipped) {
        SkipHeader();
    }

    std::string line;

    while (std::getline(_fileStream, line)) {
        if (line.empty()) continue;

        if (ParseLine(line, outRecord)) {
            return true;
        }
    }

    return false;
}
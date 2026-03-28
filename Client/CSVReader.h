#pragma once
#include <fstream>
#include <string>
#include "TelemetryRecord.h"

/**
 * @class CSVReader
 * @brief Handles reading and parsing telemetry data from a CSV file.
 */
class CSVReader {
private:
	std::string _filePath;      /**< Path to the telemetry CSV file */
	std::ifstream _fileStream;  /**< Input file stream for reading the file */
	bool _headerSkipped;        /**< Flag tracking if the CSV header row has been discarded */

	/**
	 * @brief Reads and discards the first line of the CSV file.
	 */
	void SkipHeader();

	/**
	 * @brief Parses a single CSV line into a TelemetryRecord.
	 * @param line The raw string line from the CSV.
	 * @param outRecord Reference to the TelemetryRecord to populate.
	 * @return true if the line was successfully parsed, false otherwise.
	 */
	bool ParseLine(const std::string& line, TelemetryRecord& outRecord);

public:
	/**
	 * @brief Constructs a new CSVReader and opens the file.
	 * @param path The path to the telemetry CSV file.
	 */
	CSVReader(const std::string& path);

	/**
	 * @brief Reads the next valid line from the file and populates the record.
	 * @param outRecord Reference to the TelemetryRecord to populate.
	 * @return true if a valid record was read; false on EOF or read error.
	 */
	bool ReadNext(TelemetryRecord& outRecord);
};
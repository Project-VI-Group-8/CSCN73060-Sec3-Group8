#pragma once
#include <fstream>
#include <string>
#include "TelemetryRecord.h"

/**
 * @class CSVReader
 * @brief Handles reading and parsing telemetry data from a CSV file.
 * 
 * Expected CSV format:
 *		Header row (skipped automatically)
 *		Data rows: DD_MM_YYYY HH:MM:SS,<fuel_qty>
 * 
 * Malformed lines are logged to stderr and skipped. The caller should
 * check IsOpen() before calling ReadNext().
 */
class CSVReader {
public:
	/**
	 * @brief Constructs a CSVReader and opens the file.
	 * @param path The path to the telemetry CSV file.
	 */
	explicit CSVReader(const std::string& path);

	/**
	 * @brief Reads the next valid telemetry record from the file.
	 * @param outRecord outRecord Populated on success.
	 * @return true if a valid record was read; false on EOF or unrecoverable error.
	 */
	bool ReadNext(TelemetryRecord& outRecord);
	
	/**
	  * @brief Returns true if the underlying file stream is open and healthy.
	  */
	bool IsOpen() const;
private:
	std::string		_filePath;				/**< Path to the telemetry CSV file */
	std::ifstream	_fileStream;			/**< Input file stream for reading the file */
	bool			_headerSkipped = false;	/**< Flag tracking if the CSV header row has been discarded */
	int				_lineNumber    = 0;		/**< Tracks current line for diagnostics */

	/**
	 * @brief Reads and discards the first line of the CSV file.
	 */
	void SkipHeader();

	/**
	 * @brief Parses a single CSV line into a TelemetryRecord.
	 * @param line The raw string line from the CSV.
	 * @param outRecord Reference to the TelemetryRecord to populate.
	 * @return true if the line was successfully parsed, false if the line should be skipped.
	 */
	bool ParseLine(const std::string& line, TelemetryRecord& outRecord);
	
	/**
	* @brief Removes leading and trailing whitespace characters (space, tab, newline, Carriage Return, Line Feed).
	* Safe against all-whitespace strings.
	*/
	static std::string Trim(const std::string& line);

	/**
	* @brief Validates that a timestap string matches DD_MM_YYYY HH:MM:SS.
	* Checks structure and numeric range. This method does not verify calendar correctness.
	*/
	static bool IsValidTimestamp(const std::string& timestamp);
};
#include "CSVReader.h"
#include <sstream>
#include <iostream>
#include <algorithm>

/**************************** Constructor ****************************/

CSVReader::CSVReader(const std::string& path)
    : _filePath(path), _lineNumber(0)
{
    _fileStream.open(_filePath);
    if (!_fileStream.is_open()) {
        std::cerr << "[CSVReader] Error: Could not open telemetry file at " << _filePath << "\n";
    }
}

/**************************** Public ****************************/

bool CSVReader::IsOpen() const {
    return _fileStream.is_open() && _fileStream.good();
}

bool CSVReader::ReadNext(TelemetryRecord& outRecord) {
    if (!_fileStream.is_open()) return false;

    if (!_headerSkipped) {
        SkipHeader();
    }

    std::string line;
    while (std::getline(_fileStream, line)) {
        ++_lineNumber;

        // Skip blank lines silently
        if (Trim(line).empty()) continue;

        // Valid line
        if (ParseLine(line, outRecord)) {
            return true;
        }
    }

    // Check for stream error
    if (_fileStream.bad()) {
        std::cerr << "[CSVReader] Error: Stream error while reading " << _filePath << "\n";
    }
    return false;
}

/**************************** Private ****************************/

void CSVReader::SkipHeader() {
    std::string line;
    if (std::getline(_fileStream, line)) {
        _headerSkipped = true;
        ++_lineNumber;
    }
}

bool CSVReader::ParseLine(const std::string& line, TelemetryRecord& outRecord) {
    std::stringstream ss(line);
    std::string timestampStr, fuelStr;

    if (!std::getline(ss, timestampStr, ',') || !std::getline(ss, fuelStr)) {
        std::cerr << "[CSVReader] Warning (line " << _lineNumber << "): Missing comma delimiter, skipping: " << line << "\n";
        return false;
    }

    timestampStr    = Trim(timestampStr);
    fuelStr         = Trim(fuelStr);
    
    // Strip trailing comma if there is any ("123.456   ," becomes "123.456   ")
    if (!fuelStr.empty() && fuelStr.back() == ',') {
        fuelStr.pop_back();
    }

    // Re-trim in case whitespace followed the comma ("123.456   " becomes "123.456")
    fuelStr = Trim(fuelStr);

    if (timestampStr.empty() || fuelStr.empty()) {
        std::cerr << "[CSVReader] Warning (line " << _lineNumber
            << "): Empty field(s), skipping: " << line << "\n";
        return false;
    }

    if (!IsValidTimestamp(timestampStr)) {
        std::cerr << "[CSVReader] Warning (line " << _lineNumber
            << "): Invalid timestamp \"" << timestampStr
            << "\", expected D_M_YYYY HH:MM:SS, skipping.\n";
        return false;
    }

    double fuelQty = 0.0;
    try {
        std::size_t pos = 0;
        fuelQty = std::stod(fuelStr, &pos);
    }
    catch (const std::invalid_argument&) {
        std::cerr << "[CSVReader] Warning (line " << _lineNumber
            << "): Non-numeric fuel value \"" << fuelStr << "\", skipping.\n";
        return false;
    }

    if (fuelQty < 0.0) {
        std::cerr << "[CSVReader] Warning (line " << _lineNumber
            << "): Negative fuel quantity (" << fuelQty << "), skipping.\n";
        return false;
    }

    outRecord.timestamp = timestampStr;
    outRecord.fuel_qty  = fuelQty;
    return true;
}

/**************************** Static helpers ****************************/
std::string CSVReader::Trim(const std::string& line) {
    const std::string whitespace = " \t\n\r";
    const auto start = line.find_first_not_of(whitespace);
    if (start == std::string::npos) return ""; // the line is entirely whitespace
    const auto end = line.find_last_not_of(whitespace);
    return line.substr(start, end - start + 1);
}

bool CSVReader::IsValidTimestamp(const std::string& timestamp) {
    // Expected: "D_M_YYYY HH:MM:SS"
    // Day and month can be 1 or 2 digits. Year always 4 digits. Time fields can be 1 or 2 digits.

    const auto spacePos = timestamp.find(' ');
    if (spacePos == std::string::npos) return false;

    const std::string datePart = timestamp.substr(0, spacePos);
    const std::string timePart = timestamp.substr(spacePos + 1);

    // Find the date
    std::stringstream dateSS(datePart);
    std::string dayStr, monthStr, yearStr;

    if (!std::getline(dateSS, dayStr, '_')) return false;
    if (!std::getline(dateSS, monthStr, '_')) return false;
    if (!std::getline(dateSS, yearStr)) return false;

    auto isDigitChar = [](unsigned char c) {
        return std::isdigit(c);
    };

    auto allDigits = [&](const std::string& strWithNums) {
        return !strWithNums.empty() && std::all_of(strWithNums.begin(), strWithNums.end(), isDigitChar);
    };

    if (!allDigits(dayStr) || !allDigits(monthStr) || !allDigits(yearStr)) return false;
    if (yearStr.size() != 4) return false;

    int dd      = std::stoi(dayStr);
    int mm      = std::stoi(monthStr);
    int yyyy    = std::stoi(yearStr);

    // range check
    if (dd < 1 || dd > 31)  return false;
    if (mm < 1 || mm > 12)  return false;
    if (yyyy < 0)           return false;

    // Get the time: HH:MM:SS
    std::stringstream timeSS(timePart);
    std::string hhStr, mmStr, ssStr;
    if (!std::getline(timeSS, hhStr, ':')) return false;
    if (!std::getline(timeSS, mmStr, ':')) return false;
    if (!std::getline(timeSS, ssStr)) return false;

    // check if time fields are all digits
    if (!allDigits(hhStr) || !allDigits(mmStr) || !allDigits(ssStr)) return false;

    // validity check
    if (std::stoi(hhStr) > 23) return false;
    if (std::stoi(mmStr) > 59) return false;
    if (std::stoi(ssStr) > 59) return false;

    return true;
}
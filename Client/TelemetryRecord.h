#pragma once
#include <string>

/**
 * @struct TelemetryRecord
 * @brief Represents a single snapshot of aircraft telemetry data.
 */
struct TelemetryRecord {
	std::string timestamp;  /**< Format: DD_MM_YYYY HH:MM:SS */
	double fuel_qty;        /**< Fuel quantity remaining */
};
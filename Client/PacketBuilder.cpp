#include "PacketBuilder.h"
#include <sstream>
#include <iomanip>
#include <stdexcept>

/**
 * @brief Builds a formatted packet string from the aircraft ID and telemetry record.
 */
std::string PacketBuilder::Build(int aircraftId, const TelemetryRecord& record) {
	if (aircraftId <= 0) {
		throw std::invalid_argument("[PacketBuilder] Error: aircraftId must be > 0, got "
			+ std::to_string(aircraftId));
	}
	if (record.fuel_qty < 0.0) {
		throw std::invalid_argument("[PacketBuilder] Error: fuel_qty must be >= 0, got "
			+ std::to_string(record.fuel_qty));
	}

	std::ostringstream oss;
	oss << aircraftId
		<< '|'
		<< record.timestamp
		<< '|'
		<< std::fixed << std::setprecision(2) << record.fuel_qty
		<< '\n';

	return oss.str();
}
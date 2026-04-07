#pragma once
#include <string>
#include "TelemetryRecord.h"

/**
 * @class PacketBuilder
 * @brief Serializes a TelemetryRecord into a wire-format packet string.
 *
 * Packet format: [ID]|[D_M_YYYY HH:MM:SS]|[FUEL_QTY]\n
 *
 * The timestamp is passed through as-is from the CSVReader
 * no reformatting is applied.
 */
class PacketBuilder {
public:
	/**
	 * @brief Serializes a TelemetryRecord into a transmittable packet string.
	 * @param aircraftId The unique ID assigned to this client by the server.
	 * @param record The telemetry record to serialize.
	 * @return Formatted packet string including trailing newline.
	 * @throws std::invalid_argument if aircraftId <= 0 or fuel_qty < 0.
	 */
	static std::string Build(int aircraftId, const TelemetryRecord& record);
};
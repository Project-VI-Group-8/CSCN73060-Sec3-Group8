#include <iostream>
#include "CSVReader.h"
#include "TelemetryRecord.h"

int main() {
    std::string testFile = "katl-kefd-B737-700.txt";

    CSVReader reader(testFile);
    TelemetryRecord record;

    int count = 0;

    // Read Next will skip the header automatically on the first pass
    while (reader.ReadNext(record) && count < 10) {
        std::cout << "Record " << (count + 1) << " -> "
            << "Time: [" << record.timestamp << "], "
            << "Fuel: [" << record.fuel_qty << "]" << std::endl;
        count++;
    }

    if (count == 0) {
        std::cout << "\nNo records read." << std::endl;
    }
    else {
        std::cout << "\nFound 10 records." << std::endl;
    }

    return 0;
}
# QRGenerator


This repository contains a C# console application that generates QR codes based on barcodes returned from a remote API. The project files are located in the `QRGeneratorApp` directory.

## Building

A .NET 6 SDK is required to build the application. Run the following commands:

```bash
cd QRGeneratorApp
 dotnet build
```

## Running

```bash
cd QRGeneratorApp
 dotnet run
```

The program asks for `OperationCode`, `ProductionStationCode`, `Count`, `LotNo`, and `PreLot`, requests the barcodes, and saves an `output.png` containing the generated QR codes with the `LotNo` and `PreLot` printed above them.


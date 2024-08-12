# Tcp Data Exchange Service
## Overview
**Tcp Data Exchange Service** is a multithreaded TCP service written in C# and designed to run as a Windows Service. It facilitates reliable client-server communication over TCP with a robust, customizable logging system. The service handles multiple client connections simultaneously, ensuring secure and efficient data exchange.
## Features
- **Windows Service:** Runs seamlessly as a background service on Windows, providing continuous operation.
- **Multithreaded TCP Listener:** Efficiently manages multiple simultaneous client connections.
- **Configurable Parameters:** Offers flexibility in setting TCP connection parameters and logging preferences.
- **Customizable Logging:** Supports segmented log files with configurable size limits, date stamps, and customizable separators.
- **Connection Management:** Allows clients to establish and terminate connections easily, with detailed logging of each interaction.
- **Error Handling:** Implements comprehensive error detection and handling mechanisms to ensure service reliability.
- **Timeout Management:** Automatically handles inactive connections with built-in timeout functionality.
## Getting Started
### Prerequisites
- .NET Framework or .NET Core
- Visual Studio or any compatible C# IDE
### Installation
1. Clone the repository:

        git clone https://github.com/yourusername/TcpDataExchangeService.git
3. Open the project in Visual Studio.
4. Build the solution.
5. Configure the '**config.txt**' file in the '**Properties**' directory according to your needs.
### Configuration
The service is highly configurable through a '**config.txt**' file located in the '**Properties**' directory. The following parameters can be set:

- **ip:** The IP address the server listens on.
- **port:** The TCP port the server listens on.
- **log_dir:** Directory where log files are stored ('_ProjectFolder_/_ProjectName_/log' by default).
- **log_single:** Boolean to determine if logs should be in a single file or segmented.
- **log_size:** Maximum size of each log file (e.g., "10k" for 10 KB).
- **log_date:** Boolean to include timestamps in logs.
- **log_sep:** Separator used between log entries.
- **timeout:** Connection timeout duration in milliseconds.
- **stop:** Keyword to terminate the connection ('**exit**' by deafult).
### Running the Service
1. Install it using the '**InstallUtil.exe**' utility, here are detailed instructions:
   1. [How to: Install and uninstall Windows services - Microsoft](https://learn.microsoft.com/en-us/dotnet/framework/windows-services/how-to-install-and-uninstall-services)
   2. [How to Install or Uninstall a Windows Service - C# Corner](https://www.c-sharpcorner.com/UploadFile/8f2b4a/how-to-installuninstall-net-windows-service-C-Sharp/)
2. Start the service via the Windows Services Manager or using the '**net start**' command:

       net start TcpDataExchangeService
For testing and development, you can also run it directly from Visual Studio.
### Stopping the Service
Stop the service via the Windows Services Manager or using the '**net stop**' command:

    net stop TcpDataExchangeService
## Service Management
### Creating a Connection
When a client connects to the service, a new thread is spawned to handle the communication, allowing the service to manage multiple connections concurrently. Each connection is logged with detailed information about the client’s IP address and the data exchanged.
### Stopping a Connection
To stop a connection, the client sends a message containing the stop keyword defined in the '**config.txt**' file (or, '**exit**' by default). Upon receiving this keyword, the service closes the connection gracefully and logs the event. If the connection remains idle for longer than the specified timeout duration, the service automatically closes the connection and logs it.
### Logging
Logs are generated in the directory specified in the '**config.txt**' file (or, '_ProjectFolder_/_ProjectName_/log' by default). Depending on your configuration, logs can be stored in a single file or split into multiple files with size limits. Each log entry includes detailed information about the data exchange, errors, and connection statuses.
## Contributing
Contributions are welcome! If you find any bugs or have suggestions for improvements, please feel free to create an issue or submit a pull request.
## Contact
For questions or feedback, please reach out via [LinkedIn](https://linkedin.com/in/kamran-baxshaliyev).

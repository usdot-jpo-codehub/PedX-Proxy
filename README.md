# PedX Proxy

## Project Description

The Georgia Department of Transportation (GDOT) and Atlanta Regional Commission's (ARC) Safe Trips in a Connected Transportation Network (ST-CTN) project was selected by the U.S. Department of Transportation (USDOT) as a part of the ITS4US Deployment Program. The project seeks to enhance the traveler's complete trip travel experience by enhancing mobility, reliability, and safety for system users. This is done by leveraging innovative solutions, existing deployments and team collaboration such as integrating connected vehicle (CV) data with an open-source software-based trip planner that is used to provision web-based and mobile application user access. The trip planner will provide users with the ability to create a personalized trip plan with information regarding the navigation of physical infrastructure, the ability to resolve unexpected obstacles, and ensure users' visibility throughout the trip. The proposed deployment will provide users with the ability to dynamically plan and navigate trips.

The PedX Proxy specifically serves as an interface that proxies external third-party REST calls to an internal network of Actuated Traffic Signal Controller units, enabling remote requests for pedestrian crossings. This initial release includes support for MaxTime intersection controllers (IC), however the proxy is designed with an extensible architecture to allow additional controller types to be integrated as required.

## Prerequisites

The PedX Proxy requires:

- .NET 8.0 SDK for development
- .NET 8.0 Runtime for deployment only

### System Requirements

- **Minimum Hardware Requirements**:
  - CPU: 2 cores, 2.0 GHz or higher
  - RAM: 2GB minimum, 4GB recommended
  - Disk Space: 100MB for application, 500MB with logs
  - Network: 100 Mbps Ethernet connection

### Network Requirements

- **Inbound Access**:
  - HTTPS (port 443) for client applications
  - HTTP (port 80) for redirects (optional)
- **Outbound Access**:
  - MaxTime API endpoints on HTTPS (port 443)
  - NTP servers (UDP port 123) for time synchronization

## Usage

### Building

To build the PedX Proxy:

1. Clone the repository
2. Navigate to the project directory:
   ```
   cd src/Proxy
   ```
3. Build the project:
   ```
   dotnet build
   ```

### Testing

Run the automated tests:

```
dotnet test
```

### Execution

#### Running Locally

1. Configure the application settings in the appsettings.json, security.json, and intersections.json files
2. Run the application:
   ```
   dotnet run
   ```
3. When in development mode, you can access the API documentation at: https://localhost:5001/swagger (port may vary based on configuration)

#### Deploying as a Windows Service

1. Publish the application:
   ```
   dotnet publish -c Release
   ```
2. Install as a Windows Service:
   ```
   sc create "PedX-Proxy" binPath="path\to\Proxy.exe"
   sc start "PedX-Proxy"
   ```

#### Deploying on Linux

1. Publish the application:
   ```
   dotnet publish -c Release
   ```
2. Create a systemd service file at `/etc/systemd/system/pedx-proxy.service`:
   ```
   [Unit]
   Description=PedX Proxy Service
   After=network.target

   [Service]
   WorkingDirectory=/path/to/published/app
   ExecStart=/usr/bin/dotnet /path/to/published/app/Proxy.dll
   Restart=always
   RestartSec=10
   User=www-data
   Environment=ASPNETCORE_ENVIRONMENT=Production
   Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

   [Install]
   WantedBy=multi-user.target
   ```
3. Enable and start the service:
   ```
   sudo systemctl enable pedx-proxy.service
   sudo systemctl start pedx-proxy.service
   ```
4. Check service status:
   ```
   sudo systemctl status pedx-proxy.service
   ```

#### Deploying with Docker

1. Build the Docker image:
   ```
   docker build -t pedx-proxy .
   ```

2. Run the container:
   ```
   docker run -d -p 8080:80 -p 8443:443 \
     -v /path/to/appsettings.json:/app/appsettings.json \
     -v /path/to/security.json:/app/security.json \
     -v /path/to/intersections.json:/app/intersections.json \
     --name pedx-proxy \
     pedx-proxy
   ```

3. Check container status:
   ```
   docker ps -a
   ```

4. View logs:
   ```
   docker logs pedx-proxy
   ```

#### Using Docker Compose

Create a `docker-compose.yml` file:
```yaml
version: '3.8'

services:
  pedx-proxy:
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - "8080:80"
      - "8443:443"
    volumes:
      - ./src/Proxy/appsettings.json:/app/appsettings.json
      - ./src/Proxy/security.json:/app/security.json
      - ./src/Proxy/intersections.json:/app/intersections.json
    restart: unless-stopped
```

Run with Docker Compose:
```
docker-compose up -d
```

## Additional Notes

The PedX Proxy provides an API for accessing pedestrian crossing data from traffic signal controllers. It uses API key authentication for security and includes comprehensive logging through Serilog.

### Configuration Files

The PedX Proxy uses three main configuration files:

#### appsettings.json

This file contains general application settings including logging configuration and server endpoints.

```json
{
  "Serilog": {
    "Using": ["Serilog.Sinks.File"],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore.Mvc": "Warning",
        "Microsoft.AspNetCore.Routing": "Warning",
        "Microsoft.AspNetCore.Hosting": "Warning",
        "System.Net.Http.HttpClient": "Warning"
      }
    },
    "Enrich": ["FromLogContext"],
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "C:/ProgramData/PED-X Proxy/Logs/proxy-.txt",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 31
        }
      }
    ]
  },
  "Kestrel": {
    "Endpoints": {
      "MyHttpEndpoint": {
        "Url": "http://*"
      },
      "HttpsDefaultCert": {
        "Url": "https://*"
      }
    }
  },
  "AllowedHosts": "*"
}
```

**Configuration Options:**
- **Serilog**: Configure logging behavior
  - **path**: Location where log files will be saved
  - **rollingInterval**: How often to create new log files
  - **retainedFileCountLimit**: Number of log files to keep
- **Kestrel**: Web server configuration
  - **Endpoints**: Configure HTTP and HTTPS endpoints

#### security.json

This file contains API key configurations for securing the proxy API.

```json
{
  "Security": {
    "ApiKeys": {
      "secret": {
        "Owner": "Test User",
        "Roles": ["reader", "caller"]
      },
      "readonly": {
        "Owner": "Test Read-Only User",
        "Roles": ["reader"]
      }
    }
  }
}
```

**Configuration Options:**
- **ApiKeys**: Dictionary of valid API keys
  - Each key has an **Owner** name and a list of **Roles**
  - Available roles:
    - **reader**: Can access information about intersections and crossings
    - **caller**: Can initiate pedestrian crossing requests

#### intersections.json

This file defines the traffic signal intersections and their pedestrian crossings.

```json
{
  "Intersections": {
    "241": {
      "Description": "SR 20 at Gwinnett Drive",
      "Controller": {
        "Type": "MaxTime",
        "Address": "10.10.10.10"
      },
      "Crossings": {
        "NB": { "Description": "Northbound SR 20", "Phase": 2 },
        "WB": { "Description": "Westbound Driveway", "Phase": 4 },
        "SB": { "Description": "Southbound SR 20", "Phase": 6 },
        "EB": { "Description": "Eastbound Gwinnett Drive", "Phase": 8 }
      }
    }
  }
}
```

**Configuration Options:**
- **Intersections**: Dictionary of intersection configurations
  - Keys are intersection IDs (must be unique)
  - **Description**: Human-readable description of the intersection
  - **Controller**: Traffic signal controller configuration
    - **Type**: Controller type (currently supports "MaxTime")
    - **Address**: Network address of the controller
  - **Crossings**: Dictionary of pedestrian crossings at this intersection
    - Keys are crossing IDs (unique within an intersection)
    - **Description**: Human-readable description of the crossing
    - **Phase**: Signal phase number associated with this crossing

### Using Configuration Files

1. **Installation**: 
   - By default, the application looks for configuration files in its running directory
   - For production deployments, place configuration files in `C:/ProgramData/PED-X Proxy/`

2. **Security Best Practices**:
   - In production, generate strong API keys (not "secret" or "readonly")
   - Restrict API keys to specific roles based on client needs
   - Use HTTPS with a valid SSL certificate
   - Regularly rotate API keys for sensitive operations

3. **Adding New Intersections**:
   - To add a new intersection, add a new entry to the `Intersections` dictionary in `intersections.json`
   - Ensure the intersection ID is unique
   - Configure all required crossings with their proper signal phases
   - Verify the controller address is correct and accessible from the proxy server

4. **Adding New Controller Types**:
   - Implement a new adapter class that implements the `IAdapter` interface
   - Register the new adapter in the `AdapterFactory`
   - Update the `intersections.json` file to use the new controller type


## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.

## Contributions

Please read [CONTRIBUTING.md](CONTRIBUTING.md) for details on our Code of Conduct, the process for submitting pull requests to us, and how contributions will be released.

## Contact Information

Contact GDOT for information about this repository

Contact Name: Victoria Coulter, GDOT  
Contact Information: vcoulter@dot.ga.gov

## Acknowledgements

**Citing this code**

To track how this government-funded code is used, we request that if you decide to build additional software using this code please acknowledge its Digital Object Identifier in your software's README/documentation.

> Digital Object Identifier: https://doi.org/xxx.xxx/xxxx

To cite this code in a publication or report, please cite our associated report/paper and/or our source code. Below is a sample citation for this code:

> Georgia Department of Transportation. (2025). PedX Proxy (1.0) [Source code]. Provided by ITS CodeHub through GitHub.com. Accessed 2025-06-09 from https://doi.org/xxx.xxx/xxxx.

When you copy or adapt from this code, please include the original URL you copied the source code from and date of retrieval as a comment in your code. Additional information on how to cite can be found in the [ITS CodeHub FAQ](https://its.dot.gov/code/#/faqs).

**Contributors**

Funded by USDOT JPO under the ITS4US Deployment Program.

Atlanta Regional Commission (ARC) and Georgia Department of Transportation (GDOT) - Safe Trips in a Connected Transportation Network (ST-CTN) project.


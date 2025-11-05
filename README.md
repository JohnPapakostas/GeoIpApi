# GeoIpApi — IP Address Geographic Identification API

GeoIpApi is a .NET 8 Web API that performs **geographical identification of IP addresses** using the [FreeGeoIP](https://freegeoip.app) service.  
It supports both **single IP lookup** and **asynchronous batch processing** with progress tracking and ETA estimation.

---

## Features

- Lookup single IP addresses via FreeGeoIP.app  
- Asynchronous batch geolocation for multiple IPs  
- Real-time batch progress (e.g. `20/100`) and ETA  
- Background task queue with throttled concurrency  
- Persistent storage using SQL Server & Entity Framework Core  
- Built-in Swagger documentation and testing interface  

---

## Technologies Used

| Layer | Technology |
|-------|-------------|
| Language | C# (.NET 8 Web API) |
| Database | SQL Server Express 2022 |
| ORM | Entity Framework Core 8 |
| Geolocation Service | FreeGeoIP API |
| Documentation | Swagger |
# HealthWatchful

HealthWatchful is an open-source library developed in C# that provides a wide range of health check classes for developers to use in their projects. The library is compatible with all frameworks that support .NET Standard 2.0 and aims to deliver a high-performance, adaptable solution.

## Key Features

- Compatible with all frameworks that support .NET Standard 2.0
- Offers a wide range of health check classes for various needs
- Developed and maintained by a dedicated team of experts
- High-performance and adaptable design
- Supports sending notifications to Microsoft Teams, Slack, and Discord channels via webhooks
- Easy-to-use and integrate into your projects

## Health Check Classes

HealthWatchful library contains the following classes that inherit from the IHealthCheck interface:

- ApplicationStatusHealthCheck
- CpuUsageHealthCheck
- DiskStorageHealthCheck
- FileWriteAccessHealthCheck
- FtpHealthCheck
- LdapHealthCheck
- PingHealthCheck
- SslHealthCheck
- SystemMemoryHealthCheck
- TcpHealthCheck
- WindowsServiceHealthCheck

## Sublibraries

HealthWatchful offers various sublibraries for specific situations:

- HealthWatchful.ActiveMQ: This sublibrary is used to check the health of ActiveMQ communication queues.
- HealthWatchful.Elasticsearch: This sublibrary is used to check the health status of Elasticsearch servers.
- HealthWatchful.EntityFramework: This sublibrary is used to check the health status of EntityFramework data providers.
- HealthWatchful.EntityFrameworkCore: This sublibrary is used to check the health status of EntityFramework Core data providers.
- HealthWatchful.MongoDb: Use this sublibrary to check the health of MongoDB database connections and performance.
- HealthWatchful.MsSql: This sublibrary can be used to check the health status of MsSql database.
- HealthWatchful.MySql: This sublibrary is used to check the health status of MySQL database.
- HealthWatchful.PostgreSql: Use this sublibrary to check the health status of PostgreSQL database.
- HealthWatchful.Redis: Use this sublibrary to check the health status of Redis cache.
- HealthWatchful.Sftp: Use this sublibrary to check the health status of Sftp connections.
- HealthWatchful.SignalR: Use this sublibrary to check the health status of SignalR services.
- HealthWatchfuls.Webhooks: This sublibrary allows developers to configure health check alerts through webhooks for platforms like Teams, Slack, and Discord.

Each sublibrary is specifically designed to check the health of the necessary component and implements the IHealthCheck interface.

## Getting Started

To install HealthWatchful and its sublibraries, use the following commands, replacing `<version>` with the desired version number (for example, `1.0.0`):

```bash
dotnet add package HealthWatchful --version <version>
```

```bash
dotnet add package HealthWatchful.ActiveMQ --version <version>
```

```bash
dotnet add package HealthWatchful.Elasticsearch --version <version>
```

```bash
dotnet add package HealthWatchful.EntityFramework --version <version>
```

```bash
dotnet add package HealthWatchful.EntityFrameworkCore --version <version>
```

```bash
dotnet add package HealthWatchful.MsSql --version <version>
```

```bash
dotnet add package HealthWatchful.MySql --version <version>
```

```bash
dotnet add package HealthWatchful.PostgreSql --version <version>
```

```bash
dotnet add package HealthWatchful.Redis --version <version>
```

```bash
dotnet add package HealthWatchful.Sftp --version <version>
```

```bash
dotnet add package HealthWatchful.MongoDb --version <version>
```

```bash
dotnet add package HealthWatchful.SignalR --version <version>
```

## Contributing

HealthWatchful is an open-source project, and we welcome contributions from developers like you. If you'd like to contribute to the project, please feel free to submit a pull request, report any issues you encounter, or suggest new features and enhancements.

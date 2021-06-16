# Orleans Game Leader board Demo

## Introdution

Demo using Microsoft Orleans to build a real-time game leader board example

## Setup

1. Create target DB (You can use LocalDB when run on your development machine)
2. Run Main DB init sql script: [Orleans_AdoNet_db_init/SQLServer-Main.sql](./Orleans_AdoNet_db_init/SQLServer-Main.sql)
3. Run Membership(Clustering) DB init sql script: [Orleans_AdoNet_db_init/SQLServer-Clustering.sql](./Orleans_AdoNet_db_init/SQLServer-Clustering.sql)
4. Run Persistence DB init sql script: [Orleans_AdoNet_db_init/SQLServer-Persistence.sql](./Orleans_AdoNet_db_init/SQLServer-Persistence.sql)
5. Run Reminder DB init sql script: [Orleans_AdoNet_db_init/SQLServer-Reminders.sql](./Orleans_AdoNet_db_init/SQLServer-Reminders.sql)

## Start Demo

```powershell
dotnet restore
dotnet build
dotnet run --project SiloHost/SiloHost.csproj
dotnet run --project ConsoleClient/ConsoleClient.csproj
```

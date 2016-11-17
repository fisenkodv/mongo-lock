[![Build Status](https://travis-ci.org/fisenkodv/mongo-lock.svg?branch=master)](https://travis-ci.org/fisenkodv/mongo-lock)
[![Build status](https://ci.appveyor.com/api/projects/status/gf60yj8hj666pt1h/branch/master?svg=true)](https://ci.appveyor.com/project/fisenkodv/mongo-lock/branch/master)

# MongoDB Lock
## Global lock
```csharp
var uniqueId = Guid.NewGuid().ToString();

IExclusiveLockEngine lockEngine = new ExclusiveLockEngine(
            new GlobalExclusiveLock( connectionString, TimeSpan.FromMilliseconds( 1000 ) ),
            TimeSpan.FromMilliseconds( 100 ) );

lockEngine.StartCheckingLock( uniqueId, () =>
    {
      Console.WriteLine( "Lock Acquired" );
      lockEngine.StopCheckingOrReleaseLock( uniqueId );
    }, ( reason ) =>
    {
      Console.WriteLine( "Lock lost, reason: {0}", reason );
    } );
```
## Document lock
```csharp
var uniqueId = Guid.NewGuid().ToString();

IExclusiveLockEngine lockEngine = new ExclusiveLockEngine(
            new DocumentExclusiveLock(
              connectionString,
              "builds",
              Builders<BsonDocument>.Filter.Eq( "_id", 1 ),
              TimeSpan.FromMilliseconds( 1000 ) ),
            TimeSpan.FromMilliseconds( 100 ) );

lockEngine.StartCheckingLock( uniqueId, () =>
    {
      Console.WriteLine( "Lock Acquired" );
      lockEngine.StopCheckingOrReleaseLock( uniqueId );
    }, ( reason ) =>
    {
      Console.WriteLine( "Lock lost, reason: {0}", reason );
    } );
```
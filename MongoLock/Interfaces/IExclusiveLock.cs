using System;

namespace MongoDBLock.Interfaces
{
  public interface IExclusiveLock
  {
    bool TryGetLock( string clientId );

    void ExtendLock( string clientId );

    void ReleaseLock( string clientId );

    DateTime? LastAquiredLockTime { get; }

    TimeSpan LockDuration { get; }
  }
}

using System;

namespace MongoDBLock.Interfaces
{
  public interface IExclusiveLockEngine
  {
    void StartCheckingLock( string clientId, Action onLockAcquired, Action<string> onLockLost );

    void StopCheckingOrReleaseLock( string clientId );
  }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using MongoDBLock.Interfaces;

namespace MongoDBLock
{
  public class ExclusiveLockEngine : IExclusiveLockEngine
  {
    private readonly IExclusiveLock _exclusiveLock;
    private readonly TimeSpan _lockCheckFrequency;
    private Action _onLockAcquired;
    private Action<string> _onLockLost;
    private string _clientId;
    private bool _amIHoldingTheLock;
    private readonly ManualResetEvent _waitResetEvent = new ManualResetEvent( false );

    private bool _stopWork;

    public ExclusiveLockEngine( IExclusiveLock exclusive, TimeSpan lockCheckFrequency )
    {
      _exclusiveLock = exclusive;
      _lockCheckFrequency = lockCheckFrequency;
    }

    public void StartCheckingLock( string clientId, Action onLockAcquired, Action<string> onLockLost )
    {
      if ( _onLockAcquired != null )
        throw new Exception( "Only one exclusive global lock client is supported by single engine" );

      _clientId = clientId;
      _onLockAcquired = onLockAcquired;
      _onLockLost = onLockLost;

      var taskFactory = new TaskFactory();
      taskFactory.StartNew( DoLockChecking );
    }

    public void StopCheckingOrReleaseLock( string clientId )
    {
      _stopWork = true;

      _waitResetEvent.Set();

      if ( _amIHoldingTheLock )
        _exclusiveLock.ReleaseLock( clientId );
    }

    private TimeSpan GetTimeToWait()
    {
      if ( _amIHoldingTheLock )
      {
        var intervalTillRenew = _exclusiveLock.LockDuration - _lockCheckFrequency;

        if ( intervalTillRenew.Milliseconds <= 0 )
          intervalTillRenew = _lockCheckFrequency;

        if ( _exclusiveLock.LastAquiredLockTime.HasValue )
        {
          var targetTime = _exclusiveLock.LastAquiredLockTime.Value.Add( intervalTillRenew );
          var millisecondsToWait = (int)( new TimeSpan( targetTime.Ticks - DateTime.UtcNow.Ticks ).TotalMilliseconds );

          if ( millisecondsToWait < 0 )
            millisecondsToWait = 0;

          return TimeSpan.FromMilliseconds( millisecondsToWait );
        }
      }

      return _lockCheckFrequency;
    }

    private void DoLockChecking()
    {
      while ( !_stopWork )
      {
        if ( !_amIHoldingTheLock )
          TryGetLock();
        else
          KeepLock();

        _waitResetEvent.WaitOne( GetTimeToWait() );

        if ( _stopWork )
        {
          return;
        }
      }
    }

    private void TryGetLock()
    {
      var lockAcquired = _exclusiveLock.TryGetLock( _clientId );
      if ( !_amIHoldingTheLock && lockAcquired )
      {
        _amIHoldingTheLock = true;
        _onLockAcquired();
      }
    }

    private void KeepLock()
    {
      try
      {
        _exclusiveLock.ExtendLock( _clientId );
      }
      catch ( Exception ex )
      {
        _amIHoldingTheLock = false;

        _onLockLost( ex.Message );
      }
    }
  }
}

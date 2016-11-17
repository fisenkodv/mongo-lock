using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDBLock.Interfaces;

namespace MongoDBLock
{
  public class GlobalExclusiveLock : IExclusiveLock
  {
    private const string GlobalLockId = "54235CB7-9072-4DB9-AF2C-298AE7D39AB7";
    private const string LockCollectionName = "GLOBAL_LEVEL_LOCK";

    private readonly string _connectionString;
    private IMongoCollection<ExclusiveLockModel> _collection;
    private DateTime? _lastAquiredLockTime;
    private readonly TimeSpan _lockDuration;
    private readonly object _lock = new object();

    public GlobalExclusiveLock(
      string connectionString,
      TimeSpan lockDuration )
    {
      _connectionString = connectionString;
      _lockDuration = lockDuration;
    }

    public DateTime? LastAquiredLockTime => _lastAquiredLockTime;

    public TimeSpan LockDuration => _lockDuration;

    public bool TryGetLock( string clientId )
    {
      try
      {
        InitStorage();

        var foundItem = _collection.Find( Builders<ExclusiveLockModel>.Filter.Eq( item => item.LockId, GlobalLockId ) ).FirstOrDefault();
        bool isExpired = foundItem != null && foundItem.LockAcquireTime.Add( _lockDuration ) < DateTime.UtcNow;
        if ( isExpired )
        {
          _collection.DeleteOne(
            Builders<ExclusiveLockModel>.Filter.And( new List<FilterDefinition<ExclusiveLockModel>>()
            {
              Builders<ExclusiveLockModel>.Filter.Eq(item => item.LockId, GlobalLockId),
              Builders<ExclusiveLockModel>.Filter.Eq(item => item.LockAcquireTime, foundItem.LockAcquireTime)
            } ) );
        }

        _lastAquiredLockTime = DateTime.UtcNow;

        _collection.InsertOne( new ExclusiveLockModel()
        {
          LockId = GlobalLockId,
          LockAcquireTime = _lastAquiredLockTime.Value,
          LockingProcessId = clientId
        } );

        return true;
      }
      catch ( Exception )
      {
        return false;
      }
    }

    public void ExtendLock( string clientId )
    {
      InitStorage();

      var foundItem = _collection.Find( Builders<ExclusiveLockModel>.Filter.Eq( item => item.LockId, GlobalLockId ) ).FirstOrDefault();

      if ( foundItem == null )
      {
        _lastAquiredLockTime = null;
        throw new Exception( "Lock does not exist in the database" );
      }

      if ( foundItem.LockingProcessId != clientId )
      {
        _lastAquiredLockTime = null;
        throw new Exception( "Lock is no longer hold by the process asking to prolong it" );
      }

      _lastAquiredLockTime = DateTime.UtcNow;
      foundItem.LockAcquireTime = _lastAquiredLockTime.Value;

      _collection.UpdateOne( Builders<ExclusiveLockModel>.Filter.Eq( item => item.LockId, GlobalLockId ),
        Builders<ExclusiveLockModel>.Update.Set( x => x.LockAcquireTime, _lastAquiredLockTime.Value ) );
    }

    public void ReleaseLock( string clientId )
    {
      InitStorage();

      _collection.DeleteMany(
        Builders<ExclusiveLockModel>.Filter.And( new List<FilterDefinition<ExclusiveLockModel>>()
        {
          Builders<ExclusiveLockModel>.Filter.Eq(item => item.LockId, GlobalLockId),
          Builders<ExclusiveLockModel>.Filter.Eq(item => item.LockingProcessId, clientId)
        } ) );

      _lastAquiredLockTime = null;
    }

    private void InitStorage()
    {
      lock ( _lock )
      {
        if ( _collection != null )
          return;

        _collection = Util.GetCollectionFromConnectionString<ExclusiveLockModel>(
          _connectionString,
          LockCollectionName );
      }
    }

    private class ExclusiveLockModel
    {
      [BsonId]
      public string LockId { get; set; }

      public string LockingProcessId { get; set; }

      public DateTime LockAcquireTime { get; set; }
    }
  }
}

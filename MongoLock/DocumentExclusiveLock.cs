using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDBLock.Interfaces;

namespace MongoDBLock
{
  public class DocumentExclusiveLock : IExclusiveLock
  {
    private const string LockCollectionName = "DOCUMENT_LEVEL_LOCK";

    private readonly string _connectionString;
    private readonly string _collectionName;
    private IMongoCollection<ExclusiveLockModel> _collection;
    private readonly FilterDefinition<BsonDocument> _filterDefinition;
    private string _documentId;
    private readonly object _lock = new object();
    private DateTime? _lastAquiredLockTime;
    private readonly TimeSpan _lockDuration;

    public DocumentExclusiveLock(
      string connectionString,
      string collectionName,
      FilterDefinition<BsonDocument> filterDefinition,
      TimeSpan lockDuration
      )
    {
      _connectionString = connectionString;
      _collectionName = collectionName;
      _filterDefinition = filterDefinition;
      _lockDuration = lockDuration;
    }

    public bool TryGetLock( string clientId )
    {
      try
      {
        InitStorage();

        var foundItem = _collection
          .Find( Builders<ExclusiveLockModel>.Filter.And( new List<FilterDefinition<ExclusiveLockModel>>
          {
              Builders<ExclusiveLockModel>.Filter.Eq(item => item.CollectionName, _collectionName),
              Builders<ExclusiveLockModel>.Filter.Eq(item => item.DocumentId, _documentId)
            } ) )
          .FirstOrDefault();
        bool isExpired = foundItem != null && foundItem.LockAcquireTime.Add( _lockDuration ) < DateTime.UtcNow;
        if ( isExpired )
        {
          _collection.DeleteOne(
            Builders<ExclusiveLockModel>.Filter.And( new List<FilterDefinition<ExclusiveLockModel>>
            {
              Builders<ExclusiveLockModel>.Filter.Eq(item => item.CollectionName, _collectionName),
              Builders<ExclusiveLockModel>.Filter.Eq(item => item.DocumentId, _documentId),
              Builders<ExclusiveLockModel>.Filter.Eq(item => item.LockAcquireTime, foundItem.LockAcquireTime)
            } ) );
        }

        _lastAquiredLockTime = DateTime.UtcNow;

        _collection.InsertOne( new ExclusiveLockModel()
        {
          DocumentId = _documentId,
          CollectionName = _collectionName,
          LockAcquireTime = _lastAquiredLockTime.Value,
          LockingProcessId = clientId
        } );

        return true;
      }
      catch ( Exception ex)
      {
        return false;
      }
    }

    public void ExtendLock( string clientId )
    {
      InitStorage();

      var foundItem = _collection
          .Find( Builders<ExclusiveLockModel>.Filter.And( new List<FilterDefinition<ExclusiveLockModel>>
          {
              Builders<ExclusiveLockModel>.Filter.Eq(item => item.CollectionName, _collectionName),
              Builders<ExclusiveLockModel>.Filter.Eq(item => item.DocumentId, _documentId)
            } ) )
          .FirstOrDefault();

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

      _collection.UpdateOne( Builders<ExclusiveLockModel>.Filter.
        And( new List<FilterDefinition<ExclusiveLockModel>>
          {
              Builders<ExclusiveLockModel>.Filter.Eq(item => item.CollectionName, _collectionName),
              Builders<ExclusiveLockModel>.Filter.Eq(item => item.DocumentId, _documentId)
            } ),
        Builders<ExclusiveLockModel>.Update.Set( x => x.LockAcquireTime, _lastAquiredLockTime.Value ) );
    }

    public void ReleaseLock( string clientId )
    {
      InitStorage();

      _collection.DeleteMany(
        Builders<ExclusiveLockModel>.Filter.And( new List<FilterDefinition<ExclusiveLockModel>>()
        {
          Builders<ExclusiveLockModel>.Filter.Eq(item => item.CollectionName, _collectionName),
          Builders<ExclusiveLockModel>.Filter.Eq(item => item.DocumentId, _documentId),
          Builders<ExclusiveLockModel>.Filter.Eq(item => item.LockingProcessId, clientId)
        } ) );

      _lastAquiredLockTime = null;
    }

    public DateTime? LastAquiredLockTime => _lastAquiredLockTime;

    public TimeSpan LockDuration => _lockDuration;

    private void InitStorage()
    {
      lock ( _lock )
      {
        if ( _collection != null && !string.IsNullOrEmpty( _documentId ) )
          return;

        _collection = Util.GetCollectionFromConnectionString<ExclusiveLockModel>( _connectionString, LockCollectionName );

        _documentId = Util.GetDatabaseConnectionString( _connectionString )
          .GetCollection<BsonDocument>( _collectionName )
          .Find( _filterDefinition )
          .FirstOrDefault()
          .GetElement( "_id" )
          .Value.ToString();
      }
    }

    private class ExclusiveLockModel
    {
      private const string Separator = "_#_";

      [BsonId]
      public string Id
      {
        get { return $"{DocumentId}{Separator}{CollectionName}"; }
        set
        {
          var values = value.Split( new[] { Separator }, StringSplitOptions.None );
          DocumentId = values[ 0 ];
          CollectionName = values[ 1 ];
        }
      }

      public string DocumentId { get; set; }

      public string CollectionName { get; set; }

      public string LockingProcessId { get; set; }

      public DateTime LockAcquireTime { get; set; }
    }
  }
}
using MongoDB.Driver;

namespace MongoDBLock
{
  public static class Util
  {
    private static IMongoDatabase GetDatabaseFromUrl( MongoUrl url )
    {
      var client = new MongoClient( url );
      return client.GetDatabase( url.DatabaseName );
    }

    public static IMongoDatabase GetDatabaseConnectionString( string connectionString )
    {
      return GetDatabaseFromUrl( new MongoUrl( connectionString ) );
    }

    public static IMongoCollection<TEntity> GetCollectionFromConnectionString<TEntity>(
      string connectionString,
      string collectionName )
    {
      return GetDatabaseFromUrl( new MongoUrl( connectionString ) )
          .GetCollection<TEntity>( collectionName );
    }
  }
}
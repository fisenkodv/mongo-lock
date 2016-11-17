using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDBLock;
using MongoDBLock.Interfaces;
using MongoDBLockApp.Entities;

namespace MongoDBLockApp
{
  class Program
  {
    private static bool IsGlobalLock = true;

    static void Main( string[] args )
    {
      var uniqueId = Guid.NewGuid().ToString();
      Console.WriteLine( "Starting client with unique Id {0}", uniqueId );

      var connectionString = "mongodb://localhost:27017/builds";

      Console.WriteLine( "Addign test data for client with unique Id {0}", uniqueId );
      AddBuilds( connectionString );

      IExclusiveLockEngine lockEngine =
        IsGlobalLock
          ? new ExclusiveLockEngine(
            new GlobalExclusiveLock( connectionString, TimeSpan.FromMilliseconds( 1000 ) ),
            TimeSpan.FromMilliseconds( 100 ) )
          : new ExclusiveLockEngine(
            new DocumentExclusiveLock(
              connectionString,
              "builds",
              Builders<BsonDocument>.Filter.Eq( "_id", 1 ),
              TimeSpan.FromMilliseconds( 1000 ) ),
            TimeSpan.FromMilliseconds( 100 ) );

      Console.WriteLine( "Client {0}  is trying to acquire lock", uniqueId );

      lockEngine.StartCheckingLock( uniqueId, () =>
      {
        Console.WriteLine( "Lock Acquired" );
      }, ( reason ) =>
      {
        Console.WriteLine( "Lock lost, reason: {0}", reason );
      } );

      Console.WriteLine( "Press any button to quit" );
      var keyPress = Console.ReadKey();

      if ( string.Equals( keyPress.KeyChar.ToString(), "q", StringComparison.InvariantCultureIgnoreCase ) )
        lockEngine.StopCheckingOrReleaseLock( uniqueId );
    }

    private static void AddBuilds( string connectionString )
    {
      var database = Util.GetDatabaseConnectionString( connectionString );

      List<Build> builds = new List<Build>();

      foreach ( var buildIndex in Enumerable.Range( 1, 10 ) )
      {
        List<Fixture> fixtures = new List<Fixture>();
        foreach ( var fixtureIndex in Enumerable.Range( 1, 10 ) )
        {
          var ns = string.Join( ".", Enumerable.Range( fixtureIndex, 5 ).Select( x => $"Name{x}" ) );
          var fixture = new Fixture(
            $"Assembly_{buildIndex}_{fixtureIndex}",
            $"Fixture{buildIndex}.{ns}",
            "CA",
            new ExecutionInfo( string.Empty, ExecutionStatus.NotStarted, TimeSpan.Zero ),
            Enumerable.Range( 1, 5 ).Select( x => new Test( $"Test_{x}" ) ) );

          fixtures.Add( fixture );
        }

        builds.Add( new Build( buildIndex, fixtures ) );
      }

      var collection = database.GetCollection<Build>( "builds" );
      collection.DeleteMany( Builders<Build>.Filter.Empty );
      collection.InsertMany( builds );
    }
  }
}

using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDBLockApp.Entities
{
  internal class Build
  {
    [BsonId]
    public int BuildNumber { get; set; }

    public IEnumerable<Fixture> Fixtures { get; set; }

    public Build( int buildNumber, IEnumerable<Fixture> fixtures )
    {
      BuildNumber = buildNumber;
      Fixtures = fixtures;
    }
  }
}
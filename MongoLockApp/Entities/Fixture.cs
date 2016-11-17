using System.Collections.Generic;

namespace MongoDBLockApp.Entities
{
  internal class Fixture
  {
    public string Assembly { get; private set; }
    public string Name { get; private set; }
    public string Jurisdiction { get; private set; }
    public ExecutionInfo ExecutionInfo { get; private set; }
    public IEnumerable<Test> Tests { get; private set; }

    public Fixture(
      string assembly,
      string name,
      string jurisdiction,
      ExecutionInfo executionInfo,
      IEnumerable<Test> tests )
    {
      Assembly = assembly;
      Name = name;
      Jurisdiction = jurisdiction;
      ExecutionInfo = executionInfo;
      Tests = tests;
    }
  }
}
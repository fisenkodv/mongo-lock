using System;

namespace MongoDBLockApp.Entities
{
  internal class ExecutionInfo
  {
    public string Node { get; set; }
    public ExecutionStatus Status { get; set; }
    public TimeSpan Duration { get; set; }

    public ExecutionInfo( string node, ExecutionStatus status, TimeSpan duration )
    {
      Node = node;
      Status = status;
      Duration = duration;
    }
  }
}
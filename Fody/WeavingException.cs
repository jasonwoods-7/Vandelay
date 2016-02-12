using System;
using System.Runtime.Serialization;
using Mono.Cecil.Cil;
// ReSharper disable MemberCanBeInternal

namespace Vandelay.Fody
{
  [Serializable]
  public class WeavingException : Exception
  {
    public SequencePoint SequencePoint;

    public WeavingException(string message)
      : base(message)
    {
    }

    protected WeavingException(SerializationInfo info, StreamingContext context)
      : base(info, context)
    {
    }
  }
}

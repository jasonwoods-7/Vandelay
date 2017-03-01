using System;
using System.Runtime.Serialization;
using JetBrains.Annotations;
using Mono.Cecil.Cil;

namespace Vandelay.Fody
{
  [Serializable]
  class WeavingException : Exception
  {
    [UsedImplicitly, CanBeNull]
    public SequencePoint SequencePoint;

    public WeavingException([NotNull] string message)
      : base(message)
    {
    }

    protected WeavingException([NotNull] SerializationInfo info, StreamingContext context)
      : base(info, context)
    {
    }
  }
}

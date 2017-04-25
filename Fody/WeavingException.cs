using System;
using System.Runtime.Serialization;
using JetBrains.Annotations;

namespace Vandelay.Fody
{
  [Serializable]
  class WeavingException : Exception
  {
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

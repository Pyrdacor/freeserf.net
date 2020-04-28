using System.Collections.Generic;

namespace Freeserf.Serialize
{
    /// <summary>
    /// This is used for serializable classes which contains
    /// members of a base class type that can change their
    /// types at runtime to any derived class type.
    /// </summary>
    internal interface IVirtualDataProvider : IState
    {
        List<string> ChangedVirtualDataMembers { get; }
    }
}

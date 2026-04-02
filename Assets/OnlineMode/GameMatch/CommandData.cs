using Unity.Netcode;
using UnityEngine;

namespace Assets.OnlineMode.GameMatch
{
    public struct CommandData : INetworkSerializable, System.IEquatable<CommandData>
    {
        public int WeaponType;    // cast to/from WeaponType enum
        public int X;
        public int Y;
        public int Attacker;     // cast to/from Turn enum
        public int CommandIndex;

        public void NetworkSerialize<T> (BufferSerializer<T> s) where T : IReaderWriter
        {
            s.SerializeValue(ref WeaponType);
            s.SerializeValue(ref X);
            s.SerializeValue(ref Y);
            s.SerializeValue(ref Attacker);
            s.SerializeValue(ref CommandIndex);
        }

        public bool Equals (CommandData other) =>
            WeaponType == other.WeaponType && X == other.X && Y == other.Y &&
            Attacker == other.Attacker && CommandIndex == other.CommandIndex;
    }
}
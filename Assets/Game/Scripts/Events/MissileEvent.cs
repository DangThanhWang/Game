using UnityEngine;
using Game.Events;

namespace Game.Controllers
{
    public enum MissileDestroyReason
    {
        HitTarget,
        OutOfBounds,
        Timeout
    }

    public struct MissileFiredEvent : IGameEvent
    {
        public Vector3 Position { get; }
        public float Speed { get; }

        public MissileFiredEvent(Vector3 position, float speed)
        {
            Position = position;
            Speed = speed;
        }
    }

    public struct MissileDestroyedEvent : IGameEvent
    {
        public Vector3 Position { get; }
        public MissileDestroyReason Reason { get; }

        public MissileDestroyedEvent(Vector3 position, MissileDestroyReason reason)
        {
            Position = position;
            Reason = reason;
        }
    }
}

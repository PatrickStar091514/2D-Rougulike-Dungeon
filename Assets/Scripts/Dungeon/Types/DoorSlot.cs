using System;
using UnityEngine;

namespace RogueDungeon.Dungeon.Types
{
    /// <summary>
    /// 门位标识结构体，用 (cellOffset, direction) 唯一标识房间模板上的一个门位
    /// </summary>
    [Serializable]
    public struct DoorSlot : IEquatable<DoorSlot>
    {
        [SerializeField] private Vector2Int cellOffset; // 门所在的 cell 相对坐标
        [SerializeField] private Direction direction;   // 门朝向的方向

        /// <summary>门所在的 cell 相对坐标</summary>
        public Vector2Int CellOffset => cellOffset;

        /// <summary>门朝向的方向</summary>
        public Direction Direction => direction;

        public DoorSlot(Vector2Int cellOffset, Direction direction)
        {
            this.cellOffset = cellOffset;
            this.direction = direction;
        }

        /// <summary>
        /// 判断与另一个 DoorSlot 是否相等（值比较）
        /// </summary>
        public bool Equals(DoorSlot other)
        {
            return CellOffset == other.CellOffset && Direction == other.Direction;
        }

        /// <summary>
        /// 判断与另一个对象是否相等
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is DoorSlot other && Equals(other);
        }

        /// <summary>
        /// 获取哈希值，确保相等的 DoorSlot 返回相同哈希
        /// </summary>
        public override int GetHashCode()
        {
            return HashCode.Combine(CellOffset, Direction);
        }

        public static bool operator ==(DoorSlot left, DoorSlot right) => left.Equals(right);
        public static bool operator !=(DoorSlot left, DoorSlot right) => !left.Equals(right);

        public override string ToString() => $"DoorSlot({CellOffset}, {Direction})";
    }
}

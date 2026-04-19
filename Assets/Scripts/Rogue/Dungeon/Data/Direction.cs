using UnityEngine;

namespace RogueDungeon.Rogue.Dungeon.Data
{
    /// <summary>
    /// 方向枚举，表示上下左右四个基本方向
    /// </summary>
    public enum Direction
    {
        North, // 上 (0, 1)
        South, // 下 (0, -1)
        East,  // 右 (1, 0)
        West   // 左 (-1, 0)
    }

    /// <summary>
    /// Direction 枚举的扩展方法集合
    /// </summary>
    public static class DirectionExtensions
    {
        /// <summary>
        /// 返回当前方向的反方向
        /// </summary>
        /// <returns>反方向：North↔South，East↔West</returns>
        public static Direction Opposite(this Direction dir)
        {
            return dir switch
            {
                Direction.North => Direction.South,
                Direction.South => Direction.North,
                Direction.East => Direction.West,
                Direction.West => Direction.East,
                _ => dir
            };
        }

        /// <summary>
        /// 将方向转换为对应的单位向量
        /// </summary>
        /// <returns>方向对应的 Vector2Int 单位向量</returns>
        public static Vector2Int ToVector2Int(this Direction dir)
        {
            return dir switch
            {
                Direction.North => new Vector2Int(0, 1),
                Direction.South => new Vector2Int(0, -1),
                Direction.East => new Vector2Int(1, 0),
                Direction.West => new Vector2Int(-1, 0),
                _ => Vector2Int.zero
            };
        }
    }
}

using UnityEngine;

namespace RogueDungeon.Dungeon.Types
{
    /// <summary>
    /// 房间形状枚举，定义房间占据的格子形态
    /// </summary>
    public enum RoomShape
    {
        Single,    // 单格 (1×1)
        DoubleH,   // 水平双格 (2×1)
        DoubleV,   // 垂直双格 (1×2)
        BigSquare, // 四格方形 (2×2)
        LU,        // L 形：左上延伸
        LD,        // L 形：左下延伸
        RU,        // L 形：右上延伸
        RD         // L 形：右下延伸
    }

    /// <summary>
    /// 提供 RoomShape 到 cell 坐标列表的映射方法
    /// </summary>
    public static class RoomShapeUtil
    {
        private static readonly Vector2Int[] SingleCells = { new(0, 0) };
        private static readonly Vector2Int[] DoubleHCells = { new(0, 0), new(1, 0) };
        private static readonly Vector2Int[] DoubleVCells = { new(0, 0), new(0, 1) };
        private static readonly Vector2Int[] BigSquareCells = { new(0, 0), new(1, 0), new(0, 1), new(1, 1) };
        private static readonly Vector2Int[] LUCells = { new(0, 0), new(1, 0), new(0, 1) };
        private static readonly Vector2Int[] LDCells = { new(0, 0), new(1, 0), new(0, -1) };
        private static readonly Vector2Int[] RUCells = { new(0, 0), new(1, 0), new(1, 1) };
        private static readonly Vector2Int[] RDCells = { new(0, 0), new(1, 0), new(1, -1) };

        /// <summary>
        /// 获取指定形状占据的所有 cell 相对坐标，以 (0,0) 为锚点
        /// </summary>
        /// <param name="shape">房间形状</param>
        /// <returns>该形状占据的 cell 坐标数组（新分配副本）</returns>
        public static Vector2Int[] GetCells(RoomShape shape)
        {
            Vector2Int[] source = shape switch
            {
                RoomShape.Single => SingleCells,
                RoomShape.DoubleH => DoubleHCells,
                RoomShape.DoubleV => DoubleVCells,
                RoomShape.BigSquare => BigSquareCells,
                RoomShape.LU => LUCells,
                RoomShape.LD => LDCells,
                RoomShape.RU => RUCells,
                RoomShape.RD => RDCells,
                _ => SingleCells
            };

            var result = new Vector2Int[source.Length];
            System.Array.Copy(source, result, source.Length);
            return result;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using RogueDungeon.Rogue.Dungeon.Data;

namespace RogueDungeon.Rogue.Dungeon.Runtime
{
    /// <summary>
    /// 房间实例，表示地牢中的一个房间，包含不可变的布局信息和可变的运行时状态
    /// </summary>
    public class RoomInstance
    {
        public string Id { get; }                              // 房间唯一标识
        public RoomType Type { get; }                          // 房间功能类型
        public RoomShape Shape { get; }                        // 房间形状
        public Vector2Int GridPosition { get; }                // 锚点位置
        public IReadOnlyList<Vector2Int> Cells { get; }        // 占据的 cell 列表
        public RoomTemplateSO Template { get; }                // 分配的模板
        public bool IsOnMainPath { get; }                      // 是否在主路径上
        public IReadOnlyList<DoorConnection> Doors { get; }    // 门连接列表
        public bool Visited { get; set; }                      // 是否已访问
        public bool Cleared { get; set; }                      // 是否已清除

        /// <summary>
        /// 创建房间实例
        /// </summary>
        /// <param name="id">房间唯一标识</param>
        /// <param name="type">房间功能类型</param>
        /// <param name="shape">房间形状</param>
        /// <param name="gridPosition">锚点位置</param>
        /// <param name="cells">占据的 cell 列表</param>
        /// <param name="template">分配的模板</param>
        /// <param name="isOnMainPath">是否在主路径上</param>
        /// <param name="doors">门连接列表</param>
        public RoomInstance(
            string id,
            RoomType type,
            RoomShape shape,
            Vector2Int gridPosition,
            IReadOnlyList<Vector2Int> cells,
            RoomTemplateSO template,
            bool isOnMainPath,
            IReadOnlyList<DoorConnection> doors)
        {
            Id = id;
            Type = type;
            Shape = shape;
            GridPosition = gridPosition;
            Cells = cells;
            Template = template;
            IsOnMainPath = isOnMainPath;
            Doors = doors;
            Visited = false;
            Cleared = false;
        }
    }
}

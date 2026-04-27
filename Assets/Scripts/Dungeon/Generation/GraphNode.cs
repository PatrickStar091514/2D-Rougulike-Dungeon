using System.Collections.Generic;
using UnityEngine;
using RogueDungeon.Dungeon.Types;
using RogueDungeon.Dungeon.Config;

namespace RogueDungeon.Dungeon.Generation
{
    /// <summary>
    /// 生成过程中的图节点，代表一个房间在生成树中的内部表示
    /// </summary>
    internal class GraphNode
    {
        public string Id;                     // 节点唯一标识 (room_{x}_{y})
        public Vector2Int Position;           // 锚点格子位置
        public List<Vector2Int> Cells;        // 占据的所有 cell
        public RoomType RoomType;             // 房间类型
        public RoomShape RoomShape;           // 房间形状
        public List<string> NeighborIds;      // 树边邻居 Id 列表
        public bool IsOnMainPath;             // 是否在主路径上
        public string TemplateId;             // 分配的模板 Id
        public bool IsMerged;                 // 是否已被合并（被吸收方标记 true）

        /// <summary>
        /// 创建一个初始为 Single 的图节点
        /// </summary>
        /// <param name="position">锚点格子位置</param>
        public GraphNode(Vector2Int position)
        {
            Position = position;
            Id = $"room_{position.x}_{position.y}";
            Cells = new List<Vector2Int> { position };
            RoomType = RoomType.Normal;
            RoomShape = RoomShape.Single;
            NeighborIds = new List<string>();
            IsOnMainPath = false;
            TemplateId = null;
            IsMerged = false;
        }
    }
}

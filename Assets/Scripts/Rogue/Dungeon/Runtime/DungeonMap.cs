using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RogueDungeon.Rogue.Dungeon.Data;

namespace RogueDungeon.Rogue.Dungeon.Runtime
{
    /// <summary>
    /// 地牢地图，作为地牢生成的最终输出容器
    /// </summary>
    public class DungeonMap
    {
        public string StartRoomId { get; }                     // 起始房间 Id
        public string BossRoomId { get; }                      // Boss 房间 Id
        public IReadOnlyList<RoomInstance> AllRooms { get; }    // 所有房间列表

        private readonly Dictionary<string, RoomInstance> _roomLookup; // Id -> Room 查找表

        private DungeonMap(string startRoomId, string bossRoomId, List<RoomInstance> rooms)
        {
            StartRoomId = startRoomId;
            BossRoomId = bossRoomId;
            AllRooms = rooms.AsReadOnly();
            _roomLookup = rooms.ToDictionary(r => r.Id);
        }

        /// <summary>
        /// 按 Id 查询房间
        /// </summary>
        /// <param name="id">房间 Id</param>
        /// <returns>对应的 RoomInstance，不存在时返回 null</returns>
        public RoomInstance GetRoom(string id)
        {
            return _roomLookup.TryGetValue(id, out var room) ? room : null;
        }

        /// <summary>
        /// 通过本房间的门位查询连接的目标房间
        /// </summary>
        /// <param name="roomId">当前房间 Id</param>
        /// <param name="door">当前房间的门位</param>
        /// <returns>连接的目标 RoomInstance，不存在时返回 null</returns>
        public RoomInstance GetConnectedRoom(string roomId, DoorSlot door)
        {
            var room = GetRoom(roomId);
            if (room == null) return null;

            foreach (var conn in room.Doors)
            {
                if (conn.LocalDoor.Equals(door))
                    return GetRoom(conn.ConnectedRoomId);
            }
            return null;
        }

        /// <summary>
        /// 从生成过程的 GraphNode 列表构建最终的 DungeonMap
        /// </summary>
        /// <param name="nodes">生成树节点列表</param>
        /// <param name="startId">起始房间 Id</param>
        /// <param name="bossId">Boss 房间 Id</param>
        /// <param name="config">楼层配置（用于查找模板）</param>
        /// <returns>构建完成的 DungeonMap</returns>
        internal static DungeonMap Build(
            List<Generation.GraphNode> nodes,
            string startId,
            string bossId,
            FloorConfigSO config)
        {
            // Build template lookup
            var templateLookup = new Dictionary<string, RoomTemplateSO>();
            if (config.Templates != null)
            {
                foreach (var t in config.Templates)
                {
                    if (t != null && !string.IsNullOrEmpty(t.TemplateId))
                        templateLookup[t.TemplateId] = t;
                }
            }

            // Build node lookup (non-merged only)
            var nodeLookup = new Dictionary<string, Generation.GraphNode>();
            foreach (var node in nodes)
            {
                if (!node.IsMerged)
                    nodeLookup[node.Id] = node;
            }

            // Build cell -> nodeId lookup for shared wall detection
            var cellToNodeId = new Dictionary<Vector2Int, string>();
            foreach (var node in nodes)
            {
                if (node.IsMerged) continue;
                foreach (var cell in node.Cells)
                    cellToNodeId[cell] = node.Id;
            }

            EnsureBossMainlineNeighbor(nodeLookup, cellToNodeId, templateLookup, startId, bossId);

            // Build rooms with door connections (sorted for determinism)
            var rooms = new List<RoomInstance>();
            var sortedNodes = nodes.Where(n => !n.IsMerged).OrderBy(n => n.Id).ToList();

            foreach (var node in sortedNodes)
            {
                RoomTemplateSO template = null;
                if (node.TemplateId != null)
                    templateLookup.TryGetValue(node.TemplateId, out template);

                var doors = BuildDoorConnections(node, nodeLookup, cellToNodeId, templateLookup);

                rooms.Add(new RoomInstance(
                    node.Id,
                    node.RoomType,
                    node.RoomShape,
                    node.Position,
                    node.Cells.AsReadOnly(),
                    template,
                    node.IsOnMainPath,
                    doors
                ));
            }

            EnforceBossSingleDoor(rooms, startId, bossId);

            return new DungeonMap(startId, bossId, rooms);
        }

        /// <summary>
        /// 为一个节点构建所有门连接（使用 WallEdge 归一化匹配相邻房间的门位）
        /// </summary>
        private static List<DoorConnection> BuildDoorConnections(
            Generation.GraphNode node,
            Dictionary<string, Generation.GraphNode> nodeLookup,
            Dictionary<Vector2Int, string> cellToNodeId,
            Dictionary<string, RoomTemplateSO> templateLookup)
        {
            var doors = new List<DoorConnection>();

            RoomTemplateSO template = null;
            if (node.TemplateId != null)
                templateLookup.TryGetValue(node.TemplateId, out template);

            if (template == null || template.DoorSlots == null)
                return doors;

            // For each door slot in this node's template, check if it connects to a neighbor
            foreach (var localSlot in template.DoorSlots)
            {
                var worldCell = node.Position + localSlot.CellOffset;
                var neighborCell = worldCell + localSlot.Direction.ToVector2Int();

                // Check if neighbor cell belongs to a tree-adjacent node
                if (!cellToNodeId.TryGetValue(neighborCell, out var neighborNodeId))
                    continue;
                if (neighborNodeId == node.Id)
                    continue; // Same room (multi-cell)
                if (!node.NeighborIds.Contains(neighborNodeId))
                    continue; // Not a tree neighbor
                if (!nodeLookup.TryGetValue(neighborNodeId, out var neighborNode))
                    continue;

                RoomTemplateSO neighborTemplate = null;
                if (neighborNode.TemplateId != null)
                    templateLookup.TryGetValue(neighborNode.TemplateId, out neighborTemplate);
                if (neighborTemplate == null || neighborTemplate.DoorSlots == null)
                    continue;

                // Find matching door slot in neighbor (opposite direction, correct cell offset)
                var expectedDirection = localSlot.Direction.Opposite();
                var expectedOffset = neighborCell - neighborNode.Position;

                foreach (var remoteSlot in neighborTemplate.DoorSlots)
                {
                    if (remoteSlot.CellOffset == expectedOffset && remoteSlot.Direction == expectedDirection)
                    {
                        doors.Add(new DoorConnection(localSlot, neighborNodeId, remoteSlot));
                        break;
                    }
                }
            }

            return doors;
        }

        private static void EnsureBossMainlineNeighbor(
            Dictionary<string, Generation.GraphNode> nodeLookup,
            Dictionary<Vector2Int, string> cellToNodeId,
            Dictionary<string, RoomTemplateSO> templateLookup,
            string startId,
            string bossId)
        {
            if (!nodeLookup.TryGetValue(bossId, out var bossNode))
                return;

            var adjacentCandidates = new HashSet<string>();
            var dirs = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            foreach (var bossCell in bossNode.Cells)
            {
                foreach (var dir in dirs)
                {
                    if (!cellToNodeId.TryGetValue(bossCell + dir, out var candidateId))
                        continue;
                    if (candidateId == bossId || !nodeLookup.ContainsKey(candidateId))
                        continue;
                    adjacentCandidates.Add(candidateId);
                }
            }

            if (adjacentCandidates.Count == 0)
                return;

            var existingAdjacent = bossNode.NeighborIds
                .Where(adjacentCandidates.Contains)
                .Distinct()
                .ToList();
            var candidatePool = existingAdjacent.Count > 0 ? existingAdjacent : adjacentCandidates.ToList();
            var startPos = nodeLookup.TryGetValue(startId, out var startNode) ? startNode.Position : Vector2Int.zero;

            var selectedNeighborId = candidatePool
                .OrderByDescending(id => HasMutualDoor(
                    bossNode,
                    nodeLookup[id],
                    cellToNodeId,
                    templateLookup))
                .ThenBy(id => Vector2Int.Distance(nodeLookup[id].Position, startPos))
                .ThenBy(id => id)
                .First();

            foreach (var node in nodeLookup.Values)
                node.NeighborIds.Remove(bossId);

            bossNode.NeighborIds.Clear();
            bossNode.NeighborIds.Add(selectedNeighborId);

            var selectedNode = nodeLookup[selectedNeighborId];
            if (!selectedNode.NeighborIds.Contains(bossId))
                selectedNode.NeighborIds.Add(bossId);
        }

        private static bool HasMutualDoor(
            Generation.GraphNode node,
            Generation.GraphNode neighborNode,
            Dictionary<Vector2Int, string> cellToNodeId,
            Dictionary<string, RoomTemplateSO> templateLookup)
        {
            RoomTemplateSO template = null;
            if (node.TemplateId != null)
                templateLookup.TryGetValue(node.TemplateId, out template);
            if (template == null || template.DoorSlots == null)
                return false;

            RoomTemplateSO neighborTemplate = null;
            if (neighborNode.TemplateId != null)
                templateLookup.TryGetValue(neighborNode.TemplateId, out neighborTemplate);
            if (neighborTemplate == null || neighborTemplate.DoorSlots == null)
                return false;

            foreach (var localSlot in template.DoorSlots)
            {
                var worldCell = node.Position + localSlot.CellOffset;
                var neighborCell = worldCell + localSlot.Direction.ToVector2Int();
                if (!cellToNodeId.TryGetValue(neighborCell, out var neighborId) || neighborId != neighborNode.Id)
                    continue;

                var expectedDirection = localSlot.Direction.Opposite();
                var expectedOffset = neighborCell - neighborNode.Position;
                foreach (var remoteSlot in neighborTemplate.DoorSlots)
                {
                    if (remoteSlot.CellOffset == expectedOffset && remoteSlot.Direction == expectedDirection)
                        return true;
                }
            }

            return false;
        }

        private static void EnforceBossSingleDoor(
            List<RoomInstance> rooms,
            string startId,
            string bossId)
        {
            if (rooms == null || rooms.Count == 0)
                return;

            var roomLookup = rooms.ToDictionary(r => r.Id);
            if (!roomLookup.TryGetValue(bossId, out var bossRoom))
                return;
            roomLookup.TryGetValue(startId, out var startRoom);
            startRoom ??= bossRoom;

            var candidateRooms = rooms
                .Where(r => r.Id != bossId && AreRoomsAdjacent(bossRoom, r))
                .OrderBy(r => Vector2Int.Distance(r.GridPosition, startRoom.GridPosition))
                .ThenBy(r => r.Id)
                .ToList();
            if (candidateRooms.Count == 0)
                return;

            RoomInstance selectedRoom = null;
            DoorSlot bossSlot = default;
            DoorSlot selectedSlot = default;
            foreach (var candidate in candidateRooms)
            {
                if (TryFindMutualDoorPair(bossRoom, candidate, out bossSlot, out selectedSlot))
                {
                    selectedRoom = candidate;
                    break;
                }
            }

            if (selectedRoom == null)
                return;

            var replacements = new Dictionary<string, RoomInstance>();
            foreach (var room in rooms)
            {
                if (room.Id == bossId)
                {
                    var bossDoors = new List<DoorConnection>
                    {
                        new DoorConnection(bossSlot, selectedRoom.Id, selectedSlot)
                    };
                    replacements[room.Id] = CloneWithDoors(room, bossDoors);
                    continue;
                }

                var filteredDoors = room.Doors
                    .Where(d => d.ConnectedRoomId != bossId)
                    .ToList();
                if (room.Id == selectedRoom.Id)
                {
                    filteredDoors.Add(new DoorConnection(selectedSlot, bossId, bossSlot));
                }

                if (filteredDoors.Count != room.Doors.Count || room.Id == selectedRoom.Id)
                    replacements[room.Id] = CloneWithDoors(room, filteredDoors);
            }

            for (int i = 0; i < rooms.Count; i++)
            {
                var roomId = rooms[i].Id;
                if (replacements.TryGetValue(roomId, out var updated))
                    rooms[i] = updated;
            }
        }

        private static RoomInstance CloneWithDoors(RoomInstance room, List<DoorConnection> doors)
        {
            return new RoomInstance(
                room.Id,
                room.Type,
                room.Shape,
                room.GridPosition,
                room.Cells,
                room.Template,
                room.IsOnMainPath,
                doors);
        }

        private static bool AreRoomsAdjacent(RoomInstance a, RoomInstance b)
        {
            foreach (var cellA in a.Cells)
            {
                foreach (var cellB in b.Cells)
                {
                    var delta = cellB - cellA;
                    if ((delta.x == 1 && delta.y == 0) ||
                        (delta.x == -1 && delta.y == 0) ||
                        (delta.x == 0 && delta.y == 1) ||
                        (delta.x == 0 && delta.y == -1))
                        return true;
                }
            }
            return false;
        }

        private static bool TryFindMutualDoorPair(
            RoomInstance source,
            RoomInstance target,
            out DoorSlot sourceDoor,
            out DoorSlot targetDoor)
        {
            sourceDoor = default;
            targetDoor = default;

            if (source.Template == null || source.Template.DoorSlots == null ||
                target.Template == null || target.Template.DoorSlots == null)
                return false;

            var targetCellSet = new HashSet<Vector2Int>(target.Cells);
            foreach (var localSlot in source.Template.DoorSlots)
            {
                var worldCell = source.GridPosition + localSlot.CellOffset;
                var neighborCell = worldCell + localSlot.Direction.ToVector2Int();
                if (!targetCellSet.Contains(neighborCell))
                    continue;

                var expectedDirection = localSlot.Direction.Opposite();
                var expectedOffset = neighborCell - target.GridPosition;
                foreach (var remoteSlot in target.Template.DoorSlots)
                {
                    if (remoteSlot.CellOffset == expectedOffset && remoteSlot.Direction == expectedDirection)
                    {
                        sourceDoor = localSlot;
                        targetDoor = remoteSlot;
                        return true;
                    }
                }
            }

            return false;
        }
    }
}

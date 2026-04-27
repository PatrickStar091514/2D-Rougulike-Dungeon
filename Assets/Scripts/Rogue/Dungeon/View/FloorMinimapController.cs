using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RogueDungeon.Core.Events;
using RogueDungeon.Rogue.Dungeon.Runtime;

namespace RogueDungeon.Rogue.Dungeon.View
{
    /// <summary>
    /// 楼层小地图控制器。消费地牢事件并渲染房间格、Boss 图标与清理连线。
    /// </summary>
    public class FloorMinimapController : MonoBehaviour
    {
        [Header("Bindings")]
        [SerializeField] private DungeonViewManager _viewManager; // 地牢视图管理器
        [SerializeField] private Canvas _targetCanvas; // 小地图挂载目标 Canvas
        [SerializeField] private bool _createCanvasWhenMissing = true; // 缺失 Canvas 时自动创建

        [Header("Layout")]
        [SerializeField] private Vector2 _panelSize = new(320f, 320f); // 小地图面板尺寸
        [SerializeField] private Vector2 _panelOffset = new(-24f, -24f); // 右上角偏移
        [Min(2f)]
        [SerializeField] private float _cellSize = 12f; // 单格尺寸
        [Min(0.25f)]
        [SerializeField] private float _mapScale = 1f; // 地图缩放倍率
        [Min(0f)]
        [SerializeField] private float _cellPadding = 0f; // 单格内边距
        [Min(1f)]
        [SerializeField] private float _lineThickness = 2f; // 连线粗细
        [SerializeField] private Vector2 _bossIconSize = new(16f, 16f); // Boss 图标尺寸

        [Header("Style")]
        [SerializeField] private Color _currentRoomColor = Color.white; // 当前房间颜色
        [SerializeField] private Color _visibleRoomColor = new(0.6f, 0.6f, 0.6f, 1f); // 其他可见房间颜色
        [SerializeField] private Color _lineColor = new(0.75f, 0.75f, 0.75f, 1f); // 连线颜色
        [SerializeField] private Color _panelColor = new(0f, 0f, 0f, 0.45f); // 面板底色
        [SerializeField] private Color _frameColor = new(1f, 1f, 1f, 0.85f); // 外框颜色
        [Min(0f)]
        [SerializeField] private float _frameThickness = 2f; // 外框粗细
        [SerializeField] private Sprite _bossIconSprite; // Boss 图标资源

        [Header("Runtime Debug")]
        [SerializeField] private int _debugCellVisualCount; // 当前房间格子渲染数
        [SerializeField] private int _debugLineVisualCount; // 当前连线渲染数
        [SerializeField] private string _debugCurrentRoomId; // 当前房间 ID
        [SerializeField] private string _debugBossRoomId; // Boss 房间 ID

        private RectTransform _panelRect; // 面板根节点（含 Mask）
        private RectTransform _contentRect; // 内容根节点
        private RectTransform _lineLayer; // 连线层
        private RectTransform _cellLayer; // 房间格层
        private Image _panelImage; // 面板背景图
        private Outline _panelOutline; // 面板外框
        private Image _bossIconImage; // Boss 图标

        private DungeonMap _currentMap; // 当前地牢地图
        private Vector2Int _minCell; // 全图最小 cell
        private Vector2Int _maxCell; // 全图最大 cell
        private bool _uiReady; // UI 层级是否已创建

        private readonly Dictionary<string, RoomInstance> _roomLookup = new(StringComparer.Ordinal); // roomId -> RoomInstance
        private readonly Dictionary<string, List<Image>> _roomCellViews = new(StringComparer.Ordinal); // roomId -> 房间格子视图
        private readonly List<MinimapLineVisual> _lineViews = new(); // 房间连线视图列表

        private static Sprite _fallbackBossSprite; // Boss 兜底图标

        /// <summary>
        /// 当前创建的小地图房间格子总数（测试可见）。
        /// </summary>
        internal int CellVisualCount => _debugCellVisualCount;

        /// <summary>
        /// 当前创建的小地图连线总数（测试可见）。
        /// </summary>
        internal int LineVisualCount => _debugLineVisualCount;

        /// <summary>
        /// Boss 图标是否显示（测试可见）。
        /// </summary>
        internal bool IsBossIconVisible => _bossIconImage != null && _bossIconImage.gameObject.activeSelf;

        /// <summary>
        /// 当前激活连线数量（测试可见）。
        /// </summary>
        internal int ActiveLineVisualCount
        {
            get
            {
                int count = 0;
                foreach (var line in _lineViews)
                {
                    if (line.Image != null && line.Image.gameObject.activeSelf)
                        count++;
                }
                return count;
            }
        }

        /// <summary>
        /// 获取指定房间当前渲染颜色集合（测试可见）。
        /// </summary>
        /// <param name="roomId">房间 ID</param>
        /// <param name="colors">输出颜色集合</param>
        /// <returns>是否成功获取</returns>
        internal bool TryGetRoomColors(string roomId, out List<Color> colors)
        {
            colors = new List<Color>();
            if (!_roomCellViews.TryGetValue(roomId, out var views))
                return false;

            foreach (var image in views)
            {
                if (image != null && image.gameObject.activeSelf)
                    colors.Add(image.color);
            }

            return true;
        }

        private void Awake()
        {
            if (_viewManager == null)
                _viewManager = FindFirstObjectByType<DungeonViewManager>();
        }

        private void OnEnable()
        {
            EventCenter.AddListener<DungeonGeneratedEvent>(GameEventType.DungeonGenerated, OnDungeonGenerated);
            EventCenter.AddListener<RoomEnteredEvent>(GameEventType.RoomEntered, OnRoomEntered);
            EventCenter.AddListener<RoomClearedEvent>(GameEventType.RoomCleared, OnRoomCleared);
        }

        private void OnDisable()
        {
            EventCenter.RemoveListener<DungeonGeneratedEvent>(GameEventType.DungeonGenerated, OnDungeonGenerated);
            EventCenter.RemoveListener<RoomEnteredEvent>(GameEventType.RoomEntered, OnRoomEntered);
            EventCenter.RemoveListener<RoomClearedEvent>(GameEventType.RoomCleared, OnRoomCleared);
        }

        private void OnValidate()
        {
            if (_panelSize.x < 32f) _panelSize.x = 32f;
            if (_panelSize.y < 32f) _panelSize.y = 32f;
            if (_cellSize < 2f) _cellSize = 2f;
            if (_mapScale < 0.25f) _mapScale = 0.25f;
            if (_lineThickness < 1f) _lineThickness = 1f;
            if (_frameThickness < 0f) _frameThickness = 0f;
            if (_bossIconSize.x < 4f) _bossIconSize.x = 4f;
            if (_bossIconSize.y < 4f) _bossIconSize.y = 4f;

            if (_panelRect != null && _panelImage != null)
            {
                _panelRect.sizeDelta = _panelSize;
                _panelRect.anchoredPosition = _panelOffset;
                _panelImage.color = _panelColor;
                if (_panelOutline != null)
                {
                    _panelOutline.effectColor = _frameColor;
                    _panelOutline.effectDistance = Vector2.one * _frameThickness;
                }
            }
        }

        /// <summary>
        /// 处理地牢生成事件：全量重建小地图。
        /// </summary>
        /// <param name="evt">地牢生成事件</param>
        private void OnDungeonGenerated(DungeonGeneratedEvent evt)
        {
            if (evt.Map == null || evt.Map.AllRooms == null || evt.Map.AllRooms.Count == 0)
                return;

            _currentMap = evt.Map;
            if (!EnsureUiHierarchy())
                return;

            RebuildFromMap(evt.Map);
            RefreshVisuals();
        }

        /// <summary>
        /// 处理进入房间事件：刷新房间颜色与 Boss 图标显隐。
        /// </summary>
        /// <param name="evt">进入房间事件</param>
        private void OnRoomEntered(RoomEnteredEvent evt)
        {
            if (_currentMap == null)
                return;

            RefreshVisuals();
        }

        /// <summary>
        /// 处理清房事件：刷新连线显隐。
        /// </summary>
        /// <param name="evt">清房事件</param>
        private void OnRoomCleared(RoomClearedEvent evt)
        {
            if (_currentMap == null)
                return;

            RefreshVisuals();
        }

        /// <summary>
        /// 确保小地图 UI 层级存在。若场景没有 Canvas，可按配置自动创建。
        /// </summary>
        /// <returns>是否准备完成</returns>
        private bool EnsureUiHierarchy()
        {
            if (_uiReady && _panelRect != null && _contentRect != null)
                return true;

            if (_targetCanvas == null)
                _targetCanvas = FindCanvasInLoadedScene();

            if (_targetCanvas == null && _createCanvasWhenMissing)
                _targetCanvas = CreateCanvas();

            if (_targetCanvas == null)
            {
                Debug.LogError("[FloorMinimapController] 未找到可用 Canvas，且 createCanvasWhenMissing=false");
                return false;
            }

            var panelGo = new GameObject("FloorMinimapPanel", typeof(RectTransform), typeof(Image), typeof(Mask), typeof(Outline));
            _panelRect = panelGo.GetComponent<RectTransform>();
            _panelImage = panelGo.GetComponent<Image>();
            _panelOutline = panelGo.GetComponent<Outline>();
            var panelMask = panelGo.GetComponent<Mask>();
            panelGo.transform.SetParent(_targetCanvas.transform, false);
            _panelRect.anchorMin = new Vector2(1f, 1f);
            _panelRect.anchorMax = new Vector2(1f, 1f);
            _panelRect.pivot = new Vector2(1f, 1f);
            _panelRect.anchoredPosition = _panelOffset;
            _panelRect.sizeDelta = _panelSize;
            _panelImage.sprite = GetFallbackBossSprite();
            _panelImage.type = Image.Type.Simple;
            _panelImage.preserveAspect = false;
            _panelImage.color = _panelColor;
            _panelOutline.effectColor = _frameColor;
            _panelOutline.effectDistance = Vector2.one * _frameThickness;
            _panelOutline.useGraphicAlpha = false;
            panelMask.showMaskGraphic = true;

            _contentRect = CreateLayerRect("Content", _panelRect);
            _cellLayer = CreateLayerRect("CellLayer", _contentRect);
            _lineLayer = CreateLayerRect("LineLayer", _contentRect);

            var bossGo = new GameObject("BossIcon", typeof(RectTransform), typeof(Image));
            var bossRect = bossGo.GetComponent<RectTransform>();
            _bossIconImage = bossGo.GetComponent<Image>();
            bossGo.transform.SetParent(_contentRect, false);
            bossRect.anchorMin = Vector2.zero;
            bossRect.anchorMax = Vector2.zero;
            bossRect.pivot = new Vector2(0.5f, 0.5f);
            bossRect.sizeDelta = _bossIconSize;
            _bossIconImage.raycastTarget = false;
            _bossIconImage.sprite = _bossIconSprite != null ? _bossIconSprite : GetFallbackBossSprite();
            _bossIconImage.gameObject.SetActive(false);

            _uiReady = true;
            return true;
        }

        private static RectTransform CreateLayerRect(string name, RectTransform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            go.transform.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            return rect;
        }

        private static Canvas FindCanvasInLoadedScene()
        {
            var activeCanvas = FindFirstObjectByType<Canvas>();
            if (activeCanvas != null)
                return activeCanvas;

            var all = Resources.FindObjectsOfTypeAll<Canvas>();
            foreach (var canvas in all)
            {
                if (canvas == null) continue;
                if (!canvas.gameObject.scene.IsValid()) continue;
                if (!canvas.gameObject.scene.isLoaded) continue;
                if (!canvas.gameObject.activeInHierarchy) continue;
                return canvas;
            }
            return null;
        }

        private static Canvas CreateCanvas()
        {
            var go = new GameObject("MinimapCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            return canvas;
        }

        /// <summary>
        /// 根据地图重建所有房间格子与连线缓存。
        /// </summary>
        /// <param name="map">当前地图</param>
        private void RebuildFromMap(DungeonMap map)
        {
            ClearVisuals();
            BuildRoomLookup(map);
            ComputeGlobalBounds();
            ResizeContentByBounds();
            BuildCellVisuals();
            BuildLineVisuals();
            _debugBossRoomId = map.BossRoomId;
        }

        private void BuildRoomLookup(DungeonMap map)
        {
            _roomLookup.Clear();
            foreach (var room in map.AllRooms)
            {
                if (room != null && !string.IsNullOrEmpty(room.Id))
                    _roomLookup[room.Id] = room;
            }
        }

        private void ComputeGlobalBounds()
        {
            int minX = int.MaxValue;
            int minY = int.MaxValue;
            int maxX = int.MinValue;
            int maxY = int.MinValue;

            foreach (var room in _roomLookup.Values)
            {
                foreach (var cell in room.Cells)
                {
                    if (cell.x < minX) minX = cell.x;
                    if (cell.y < minY) minY = cell.y;
                    if (cell.x > maxX) maxX = cell.x;
                    if (cell.y > maxY) maxY = cell.y;
                }
            }

            _minCell = new Vector2Int(minX, minY);
            _maxCell = new Vector2Int(maxX, maxY);
        }

        private void ResizeContentByBounds()
        {
            var width = (_maxCell.x - _minCell.x + 1) * EffectiveCellSize;
            var height = (_maxCell.y - _minCell.y + 1) * EffectiveCellSize;
            _contentRect.sizeDelta = new Vector2(width, height);
            _lineLayer.sizeDelta = _contentRect.sizeDelta;
            _cellLayer.sizeDelta = _contentRect.sizeDelta;
        }

        private void BuildCellVisuals()
        {
            foreach (var room in _roomLookup.Values)
            {
                var cellViews = new List<Image>();
                foreach (var cell in room.Cells)
                {
                    var cellGo = new GameObject($"Cell_{room.Id}_{cell.x}_{cell.y}", typeof(RectTransform), typeof(Image));
                    var cellRect = cellGo.GetComponent<RectTransform>();
                    var cellImage = cellGo.GetComponent<Image>();
                    cellGo.transform.SetParent(_cellLayer, false);

                    cellRect.anchorMin = Vector2.zero;
                    cellRect.anchorMax = Vector2.zero;
                    cellRect.pivot = new Vector2(0.5f, 0.5f);
                    cellRect.sizeDelta = Vector2.one * Mathf.Max(1f, EffectiveCellSize - EffectiveCellPadding);
                    cellRect.anchoredPosition = ToCellAnchorPosition(cell);
                    cellImage.raycastTarget = false;

                    cellViews.Add(cellImage);
                }

                _roomCellViews[room.Id] = cellViews;
            }
        }

        private void BuildLineVisuals()
        {
            var edgeKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var room in _roomLookup.Values)
            {
                foreach (var door in room.Doors)
                {
                    if (string.IsNullOrEmpty(door.ConnectedRoomId)) continue;
                    if (!_roomLookup.ContainsKey(door.ConnectedRoomId)) continue;

                    string a = room.Id;
                    string b = door.ConnectedRoomId;
                    string key = BuildUndirectedEdgeKey(a, b);
                    if (!edgeKeys.Add(key))
                        continue;

                    var lineGo = new GameObject($"Line_{key}", typeof(RectTransform), typeof(Image));
                    var lineRect = lineGo.GetComponent<RectTransform>();
                    var lineImage = lineGo.GetComponent<Image>();
                    lineGo.transform.SetParent(_lineLayer, false);
                    lineRect.anchorMin = Vector2.zero;
                    lineRect.anchorMax = Vector2.zero;
                    lineRect.pivot = new Vector2(0.5f, 0.5f);
                    lineImage.raycastTarget = false;

                    _lineViews.Add(new MinimapLineVisual(a, b, lineImage));
                }
            }
        }

        private static string BuildUndirectedEdgeKey(string roomA, string roomB)
        {
            return string.CompareOrdinal(roomA, roomB) <= 0
                ? $"{roomA}|{roomB}"
                : $"{roomB}|{roomA}";
        }

        private void RefreshVisuals()
        {
            if (_currentMap == null)
                return;

            string currentRoomId = DungeonManager.Instance?.CurrentRoom?.Id;
            if (string.IsNullOrEmpty(currentRoomId))
                currentRoomId = _currentMap.StartRoomId;
            _debugCurrentRoomId = currentRoomId;
            CenterContentOnRoom(currentRoomId);

            RefreshRoomCells(currentRoomId);
            RefreshBossIcon();
            RefreshLines();
            SyncDebugCounters();
        }

        private void CenterContentOnRoom(string roomId)
        {
            if (_contentRect == null || _panelRect == null)
                return;
            if (string.IsNullOrEmpty(roomId))
                return;
            if (!_roomLookup.TryGetValue(roomId, out var room))
                return;

            var targetCenter = ComputeRoomCenterPosition(room);
            float targetX = (_panelSize.x * 0.5f) - targetCenter.x;
            float targetY = (_panelSize.y * 0.5f) - targetCenter.y;

            var contentSize = _contentRect.sizeDelta;
            float minX = Mathf.Min(0f, _panelSize.x - contentSize.x);
            float minY = Mathf.Min(0f, _panelSize.y - contentSize.y);
            float clampedX = contentSize.x <= _panelSize.x
                ? (_panelSize.x - contentSize.x) * 0.5f
                : Mathf.Clamp(targetX, minX, 0f);
            float clampedY = contentSize.y <= _panelSize.y
                ? (_panelSize.y - contentSize.y) * 0.5f
                : Mathf.Clamp(targetY, minY, 0f);

            _contentRect.anchoredPosition = new Vector2(clampedX, clampedY);
        }

        private void RefreshRoomCells(string currentRoomId)
        {
            foreach (var kv in _roomCellViews)
            {
                bool isVisible = IsRoomVisible(kv.Key);
                bool isCurrent = isVisible && string.Equals(kv.Key, currentRoomId, StringComparison.Ordinal);
                var targetColor = isCurrent ? _currentRoomColor : _visibleRoomColor;

                foreach (var image in kv.Value)
                {
                    if (image == null) continue;
                    image.gameObject.SetActive(isVisible);
                    if (isVisible)
                        image.color = targetColor;
                }
            }
        }

        private void RefreshBossIcon()
        {
            if (_bossIconImage == null || _currentMap == null || string.IsNullOrEmpty(_currentMap.BossRoomId))
                return;

            if (!_roomLookup.TryGetValue(_currentMap.BossRoomId, out var bossRoom))
            {
                _bossIconImage.gameObject.SetActive(false);
                return;
            }

            bool isVisible = IsRoomVisible(bossRoom.Id);
            _bossIconImage.gameObject.SetActive(isVisible);
            _bossIconImage.sprite = _bossIconSprite != null ? _bossIconSprite : GetFallbackBossSprite();
            _bossIconImage.rectTransform.sizeDelta = _bossIconSize * _mapScale;
            if (!isVisible)
                return;

            _bossIconImage.rectTransform.anchoredPosition = ComputeRoomCenterPosition(bossRoom);
        }

        private void RefreshLines()
        {
            foreach (var line in _lineViews)
            {
                if (line.Image == null) continue;
                if (!_roomLookup.TryGetValue(line.RoomA, out var roomA)) continue;
                if (!_roomLookup.TryGetValue(line.RoomB, out var roomB)) continue;

                bool visible = IsRoomVisible(roomA.Id) && IsRoomVisible(roomB.Id);
                bool cleared = IsRoomCleared(roomA.Id) && IsRoomCleared(roomB.Id);
                bool active = visible && cleared;
                line.Image.gameObject.SetActive(active);
                if (!active) continue;

                line.Image.color = _lineColor;
                var start = ComputeRoomCenterPosition(roomA);
                var end = ComputeRoomCenterPosition(roomB);
                UpdateLineRect(line.Image.rectTransform, start, end);
            }
        }

        private bool IsRoomVisible(string roomId)
        {
            if (_viewManager != null && _viewManager.TryGetRoomView(roomId, out var view) && view != null)
                return view.Visibility != RoomVisibility.Hidden;

            // 回退：缺失 RoomView 时，起始房间与已访问房间视为可见。
            if (_currentMap != null && string.Equals(roomId, _currentMap.StartRoomId, StringComparison.Ordinal))
                return true;
            return _roomLookup.TryGetValue(roomId, out var room) && room.Visited;
        }

        private bool IsRoomCleared(string roomId)
        {
            if (_currentMap != null && string.Equals(roomId, _currentMap.StartRoomId, StringComparison.Ordinal))
                return true;
            return _roomLookup.TryGetValue(roomId, out var room) && room.Cleared;
        }

        private Vector2 ComputeRoomCenterPosition(RoomInstance room)
        {
            if (room == null || room.Cells == null || room.Cells.Count == 0)
                return Vector2.zero;

            Vector2 sum = Vector2.zero;
            for (int i = 0; i < room.Cells.Count; i++)
                sum += ToCellAnchorPosition(room.Cells[i]);
            return sum / room.Cells.Count;
        }

        private Vector2 ToCellAnchorPosition(Vector2Int cell)
        {
            float x = (cell.x - _minCell.x + 0.5f) * EffectiveCellSize;
            float y = (cell.y - _minCell.y + 0.5f) * EffectiveCellSize;
            return new Vector2(x, y);
        }

        private void UpdateLineRect(RectTransform lineRect, Vector2 from, Vector2 to)
        {
            var delta = to - from;
            float length = delta.magnitude;
            lineRect.anchoredPosition = (from + to) * 0.5f;
            lineRect.sizeDelta = new Vector2(length, EffectiveLineThickness);
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            lineRect.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        private float EffectiveCellSize => _cellSize * _mapScale;
        private float EffectiveCellPadding => _cellPadding * _mapScale;
        private float EffectiveLineThickness => _lineThickness * _mapScale;

        private void SyncDebugCounters()
        {
            _debugCellVisualCount = 0;
            foreach (var kv in _roomCellViews)
            {
                foreach (var image in kv.Value)
                {
                    if (image != null)
                        _debugCellVisualCount++;
                }
            }
            _debugLineVisualCount = _lineViews.Count;
        }

        private void ClearVisuals()
        {
            foreach (Transform child in _cellLayer)
                Destroy(child.gameObject);
            foreach (Transform child in _lineLayer)
                Destroy(child.gameObject);

            _roomCellViews.Clear();
            _lineViews.Clear();
            _debugCellVisualCount = 0;
            _debugLineVisualCount = 0;
        }

        private static Sprite GetFallbackBossSprite()
        {
            if (_fallbackBossSprite != null)
                return _fallbackBossSprite;

            var tex = Texture2D.whiteTexture;
            _fallbackBossSprite = Sprite.Create(
                tex,
                new Rect(0f, 0f, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                tex.width);
            return _fallbackBossSprite;
        }

        private sealed class MinimapLineVisual
        {
            public string RoomA { get; } // 连线端点 A
            public string RoomB { get; } // 连线端点 B
            public Image Image { get; } // 连线图像

            public MinimapLineVisual(string roomA, string roomB, Image image)
            {
                RoomA = roomA;
                RoomB = roomB;
                Image = image;
            }
        }
    }
}

using UnityEngine;

namespace RogueDungeon.Rogue.Dungeon.Data
{
    /// <summary>
    /// 房间模板 ScriptableObject，定义单个房间模板的形状、门位、Prefab 等信息
    /// </summary>
    [CreateAssetMenu(fileName = "NewRoomTemplate", menuName = "Dungeon/Room Template")]
    public class RoomTemplateSO : ScriptableObject
    {
        [SerializeField] private string templateId;       // 模板唯一标识
        [SerializeField] private RoomShape shape;         // 房间形状
        [SerializeField] private RoomType[] allowedTypes; // 允许的房间功能类型
        [SerializeField] private DoorSlot[] doorSlots;    // 门位列表
        [SerializeField] private GameObject prefab;       // 房间 Prefab 引用
        [SerializeField] private int weight = 1;          // 选择权重 (≥1)

        /// <summary>模板唯一标识</summary>
        public string TemplateId => templateId;

        /// <summary>房间形状</summary>
        public RoomShape Shape => shape;

        /// <summary>允许的房间功能类型</summary>
        public RoomType[] AllowedTypes => allowedTypes;

        /// <summary>门位列表</summary>
        public DoorSlot[] DoorSlots => doorSlots;

        /// <summary>房间 Prefab 引用</summary>
        public GameObject Prefab => prefab;

        /// <summary>选择权重 (≥1)</summary>
        public int Weight => weight;
    }
}

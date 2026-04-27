using System.Collections.Generic;
using UnityEngine;

namespace RogueDungeon.Dungeon.Reward
{
    /// <summary>
    /// 互斥拾取组。组内任一掉落物被拾取后，其余成员全部回收。
    /// </summary>
    public class ExclusivePickupGroup : MonoBehaviour
    {
        private readonly List<BuffDrop> _members = new();
        private bool _claimed;

        /// <summary>
        /// 初始化互斥组成员。
        /// </summary>
        /// <param name="members">同组掉落物列表</param>
        public void Init(List<BuffDrop> members)
        {
            _members.Clear();
            if (members != null)
                _members.AddRange(members);
            _claimed = false;
        }

        /// <summary>
        /// 处理成员被拾取事件，确保仅一次生效并回收其余成员。
        /// </summary>
        /// <param name="picked">被拾取成员</param>
        public void OnMemberPicked(BuffDrop picked)
        {
            if (_claimed) return;
            _claimed = true;

            for (int i = 0; i < _members.Count; i++)
            {
                var member = _members[i];
                if (member == null || member == picked) continue;
                member.ReleaseFromGroup();
            }
        }
    }
}

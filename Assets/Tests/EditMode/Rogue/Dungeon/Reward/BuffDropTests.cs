using NUnit.Framework;
using UnityEngine;
using RogueDungeon.Rogue.Dungeon.Reward;

namespace RogueDungeon.Tests.Dungeon.Reward
{
    public class BuffDropTests
    {
        private GameObject _dropGo;

        [TearDown]
        public void TearDown()
        {
            if (_dropGo != null) Object.DestroyImmediate(_dropGo);
        }

        [Test]
        public void Init_AssignsIcon_AndRaisesSortingOrderForVisibility()
        {
            _dropGo = new GameObject("BuffDrop_Test");
            var renderer = _dropGo.AddComponent<SpriteRenderer>();
            var collider = _dropGo.AddComponent<CircleCollider2D>();
            var drop = _dropGo.AddComponent<BuffDrop>();

            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);

            drop.Init(
                "buff_attack_flat",
                sprite,
                null,
                null,
                "Drop",
                1.25f,
                0.8f);

            Assert.AreEqual(sprite, renderer.sprite);
            Assert.IsTrue(renderer.enabled);
            Assert.AreEqual("Drop", renderer.sortingLayerName);
            Assert.GreaterOrEqual(renderer.sortingOrder, 900);
            Assert.IsTrue(collider.enabled);
            Assert.IsTrue(collider.isTrigger);
            Assert.AreEqual(0.8f, collider.radius, 0.001f);
            Assert.AreEqual(1.25f, _dropGo.transform.localScale.x, 0.001f);
            Assert.AreEqual(1.25f, _dropGo.transform.localScale.y, 0.001f);
        }
    }
}

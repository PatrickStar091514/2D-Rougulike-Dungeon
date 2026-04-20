using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using RogueDungeon.Rogue.Dungeon.View;

namespace RogueDungeon.Tests.Dungeon.View
{
    public class SimpleFogControllerTests
    {
        private static readonly int ColorProp = Shader.PropertyToID("_Color");
        private GameObject _root;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("TestRoom");
            // 添加子 Tilemap 以模拟真实 Prefab 结构
            var tilemapGo = new GameObject("Ground");
            tilemapGo.transform.SetParent(_root.transform);
            tilemapGo.AddComponent<Tilemap>();
            tilemapGo.AddComponent<TilemapRenderer>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
        }

        [Test]
        public void ApplyVisibility_Hidden_DeactivatesRoot()
        {
            var fog = new SimpleFogController(_root);
            fog.ApplyVisibility(RoomVisibility.Hidden);
            Assert.IsFalse(_root.activeSelf);
        }

        [Test]
        public void ApplyVisibility_Silhouette_ActivatesRootWithDarkTint()
        {
            var fog = new SimpleFogController(_root);
            _root.SetActive(false);
            fog.ApplyVisibility(RoomVisibility.Silhouette);
            Assert.IsTrue(_root.activeSelf);

            var renderer = _root.GetComponentInChildren<TilemapRenderer>();
            Assert.IsNotNull(renderer);
            var mpb = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(mpb);
            // 暗色 tint 不应为白色
            Assert.AreNotEqual(Color.white, mpb.GetColor(ColorProp));
        }

        [Test]
        public void ApplyVisibility_Revealed_ActivatesRootWithWhiteColor()
        {
            var fog = new SimpleFogController(_root);
            _root.SetActive(false);
            fog.ApplyVisibility(RoomVisibility.Revealed);
            Assert.IsTrue(_root.activeSelf);

            var renderer = _root.GetComponentInChildren<TilemapRenderer>();
            Assert.IsNotNull(renderer);
            var mpb = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(mpb);
            Assert.AreEqual(Color.white, mpb.GetColor(ColorProp));
        }

        [Test]
        public void ApplyVisibility_SilhouetteThenRevealed_ColorChanges()
        {
            var fog = new SimpleFogController(_root);
            fog.ApplyVisibility(RoomVisibility.Silhouette);

            var renderer = _root.GetComponentInChildren<TilemapRenderer>();
            var mpb = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(mpb);
            var silhouetteColor = mpb.GetColor(ColorProp);

            fog.ApplyVisibility(RoomVisibility.Revealed);
            renderer.GetPropertyBlock(mpb);
            Assert.AreEqual(Color.white, mpb.GetColor(ColorProp));
            Assert.AreNotEqual(silhouetteColor, Color.white);
        }
    }
}

using NUnit.Framework;
using RogueDungeon.Rogue.Dungeon.Data;
using UnityEngine;

namespace RogueDungeon.Tests.Dungeon
{
    public class RoomShapeUtilTests
    {
        [Test]
        public void GetCells_Single_Returns1Cell()
        {
            var cells = RoomShapeUtil.GetCells(RoomShape.Single);
            Assert.AreEqual(1, cells.Length);
            CollectionAssert.Contains(cells, new Vector2Int(0, 0));
        }

        [Test]
        public void GetCells_DoubleH_Returns2Cells()
        {
            var cells = RoomShapeUtil.GetCells(RoomShape.DoubleH);
            Assert.AreEqual(2, cells.Length);
            CollectionAssert.Contains(cells, new Vector2Int(0, 0));
            CollectionAssert.Contains(cells, new Vector2Int(1, 0));
        }

        [Test]
        public void GetCells_DoubleV_Returns2Cells()
        {
            var cells = RoomShapeUtil.GetCells(RoomShape.DoubleV);
            Assert.AreEqual(2, cells.Length);
            CollectionAssert.Contains(cells, new Vector2Int(0, 0));
            CollectionAssert.Contains(cells, new Vector2Int(0, 1));
        }

        [Test]
        public void GetCells_BigSquare_Returns4Cells()
        {
            var cells = RoomShapeUtil.GetCells(RoomShape.BigSquare);
            Assert.AreEqual(4, cells.Length);
            CollectionAssert.Contains(cells, new Vector2Int(0, 0));
            CollectionAssert.Contains(cells, new Vector2Int(1, 0));
            CollectionAssert.Contains(cells, new Vector2Int(0, 1));
            CollectionAssert.Contains(cells, new Vector2Int(1, 1));
        }

        [Test]
        public void GetCells_LU_Returns3Cells()
        {
            var cells = RoomShapeUtil.GetCells(RoomShape.LU);
            Assert.AreEqual(3, cells.Length);
            CollectionAssert.Contains(cells, new Vector2Int(0, 0));
            CollectionAssert.Contains(cells, new Vector2Int(1, 0));
            CollectionAssert.Contains(cells, new Vector2Int(0, 1));
        }

        [Test]
        public void GetCells_LD_Returns3Cells()
        {
            var cells = RoomShapeUtil.GetCells(RoomShape.LD);
            Assert.AreEqual(3, cells.Length);
            CollectionAssert.Contains(cells, new Vector2Int(0, 0));
            CollectionAssert.Contains(cells, new Vector2Int(1, 0));
            CollectionAssert.Contains(cells, new Vector2Int(0, -1));
        }

        [Test]
        public void GetCells_RU_Returns3Cells()
        {
            var cells = RoomShapeUtil.GetCells(RoomShape.RU);
            Assert.AreEqual(3, cells.Length);
            CollectionAssert.Contains(cells, new Vector2Int(0, 0));
            CollectionAssert.Contains(cells, new Vector2Int(1, 0));
            CollectionAssert.Contains(cells, new Vector2Int(1, 1));
        }

        [Test]
        public void GetCells_RD_Returns3Cells()
        {
            var cells = RoomShapeUtil.GetCells(RoomShape.RD);
            Assert.AreEqual(3, cells.Length);
            CollectionAssert.Contains(cells, new Vector2Int(0, 0));
            CollectionAssert.Contains(cells, new Vector2Int(1, 0));
            CollectionAssert.Contains(cells, new Vector2Int(1, -1));
        }

        [Test]
        public void GetCells_AllShapes_ReturnNonEmptyArrays(
            [Values] RoomShape shape)
        {
            var cells = RoomShapeUtil.GetCells(shape);
            Assert.IsNotNull(cells);
            Assert.Greater(cells.Length, 0);
            CollectionAssert.Contains(cells, new Vector2Int(0, 0));
        }

        [Test]
        public void GetCells_ReturnsNewArrayEachCall()
        {
            var a = RoomShapeUtil.GetCells(RoomShape.Single);
            var b = RoomShapeUtil.GetCells(RoomShape.Single);
            Assert.AreNotSame(a, b, "每次调用应返回新数组副本");
        }
    }
}

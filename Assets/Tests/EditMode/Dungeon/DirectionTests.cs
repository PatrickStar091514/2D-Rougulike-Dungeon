using NUnit.Framework;
using RogueDungeon.Rogue.Dungeon.Data;
using UnityEngine;

namespace RogueDungeon.Tests.Dungeon
{
    public class DirectionTests
    {
        [Test]
        public void Opposite_North_ReturnsSouth()
        {
            Assert.AreEqual(Direction.South, Direction.North.Opposite());
        }

        [Test]
        public void Opposite_South_ReturnsNorth()
        {
            Assert.AreEqual(Direction.North, Direction.South.Opposite());
        }

        [Test]
        public void Opposite_East_ReturnsWest()
        {
            Assert.AreEqual(Direction.West, Direction.East.Opposite());
        }

        [Test]
        public void Opposite_West_ReturnsEast()
        {
            Assert.AreEqual(Direction.East, Direction.West.Opposite());
        }

        [Test]
        public void Opposite_DoubleInverse_ReturnsSelf(
            [Values(Direction.North, Direction.South, Direction.East, Direction.West)]
            Direction dir)
        {
            Assert.AreEqual(dir, dir.Opposite().Opposite());
        }

        [Test]
        public void ToVector2Int_North_Returns_0_1()
        {
            Assert.AreEqual(new Vector2Int(0, 1), Direction.North.ToVector2Int());
        }

        [Test]
        public void ToVector2Int_South_Returns_0_Neg1()
        {
            Assert.AreEqual(new Vector2Int(0, -1), Direction.South.ToVector2Int());
        }

        [Test]
        public void ToVector2Int_East_Returns_1_0()
        {
            Assert.AreEqual(new Vector2Int(1, 0), Direction.East.ToVector2Int());
        }

        [Test]
        public void ToVector2Int_West_Returns_Neg1_0()
        {
            Assert.AreEqual(new Vector2Int(-1, 0), Direction.West.ToVector2Int());
        }
    }
}

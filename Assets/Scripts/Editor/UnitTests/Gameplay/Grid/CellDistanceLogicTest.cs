using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Gameplay.Grid {
    /// <summary>
    /// Unit tests for <see cref="CellDistanceLogic"/>
    /// </summary>
    [TestFixture]
    public class CellDistanceLogicTest {
        
        [Test]
        [TestCase(0, 7, 5, 7, true)]
        [TestCase(0, 0, -5, 0, true)]
        [TestCase(0, 7, 0, 7, true)]
        [TestCase(0, 0, 0, 1, true)]
        [TestCase(0, 0, 1, 2, true)]
        [TestCase(0, 0, 1, 3, true)]
        [TestCase(0, 0, 2, 4, true)]
        [TestCase(0, 0, 2, 5, true)]
        [TestCase(-1, -2, 2, 5, true)]
        [TestCase(-1, -2, -1, -1, true)]
        [TestCase(-1, -2, 0, 0, true)]
        [TestCase(-1, 3, 1, 0, true)]
        [TestCase(1, 0, -1, 3, true)]
        [TestCase(-1, 3, 0, 1, true)]
        [TestCase(-1, 3, 0, 2, true)]
        [TestCase(-1, 1, 0, 3, true)]
        [TestCase(0, 3, -1, 1, true)]
        [TestCase(2, 0, 0, 3, true)]
        [TestCase(0, 3, 2, 0, true)]
        [TestCase(-1, 3, -1, 1, false)]
        [TestCase(-1, 3, 1, 2, false)]
        [TestCase(-1, 3, 1, 1, false)]
        [TestCase(-1, 3, 2, 0, false)]
        [TestCase(1, 0, 1, 3, false)]
        [TestCase(-1, -2, 1, 1, false)]
        [TestCase(-1, -2, -1, 1, false)]
        [TestCase(2, 0, -1, 1, false)]
        [TestCase(2, 0, -1, 3, false)]
        public void TestAreCellsInStraightLine(int x1, int y1, int x2, int y2, bool expected) {
            Assert.AreEqual(expected, CellDistanceLogic.AreCellsInStraightLine(new Vector2Int(x1, y1), new Vector2Int(x2, y2)));
        }

        [Test]
        [TestCase(0, 7, 5, 7, "1,7 2,7 3,7 4,7 5,7")]
        [TestCase(0, 0, -5, 0, "-1,0 -2,0 -3,0 -4,0 -5,0")]
        [TestCase(0, 7, 0, 7, null)]
        [TestCase(0, 0, 0, 1, "0,1")]
        [TestCase(0, 0, 1, 2, "0,1 1,2")]
        [TestCase(0, 0, 1, 3, "0,1 1,2 1,3")]
        [TestCase(0, 0, 2, 4, "0,1 1,2 1,3 2,4")]
        [TestCase(0, 0, 2, 5, "0,1 1,2 1,3 2,4 2,5")]
        [TestCase(-1, -2, 2, 5, "-1,-1 0,0 0,1 1,2 1,3 2,4 2,5")]
        [TestCase(-1, -2, -1, -1, "-1,-1")]
        [TestCase(-1, -2, 0, 0, "-1,-1 0,0")]
        [TestCase(-1, 3, 1, 0, "0,2 0,1 1,0")]
        [TestCase(1, 0, -1, 3, "0,1 0,2 -1,3")]
        [TestCase(-1, 3, 0, 1, "0,2 0,1")]
        [TestCase(-1, 3, 0, 2, "0,2")]
        [TestCase(-1, 1, 0, 3, "0,2 0,3")]
        [TestCase(0, 3, -1, 1, "0,2 -1,1")]
        [TestCase(2, 0, 0, 3, "1,1 1,2 0,3")]
        [TestCase(0, 3, 2, 0, "1,2 1,1 2,0")]
        [TestCase(-1, 3, -1, 1, null)]
        [TestCase(-1, 3, 1, 2, null)]
        [TestCase(-1, 3, 1, 1, null)]
        [TestCase(-1, 3, 2, 0, null)]
        [TestCase(1, 0, 1, 3, null)]
        [TestCase(-1, -2, 1, 1, null)]
        [TestCase(-1, -2, -1, 1, null)]
        [TestCase(2, 0, -1, 1, null)]
        [TestCase(2, 0, -1, 3, null)]        
        public void TestGetCellsInStraightLine(int x1, int y1, int x2, int y2, string expectedOutputString) {
            List<Vector2Int> expectedCells = expectedOutputString?.Split(' ')
                .Select(s => {
                    string[] numberStrings = s.Split(',').ToArray();
                    int x = int.Parse(numberStrings[0]);
                    int y = int.Parse(numberStrings[1]);
                    return new Vector2Int(x, y);
                })
                .ToList();

            Assert.AreEqual(expectedCells, CellDistanceLogic.GetCellsInStraightLine(new Vector2Int(x1, y1), new Vector2Int(x2, y2)));
        }
    }
}
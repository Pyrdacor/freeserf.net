using Freeserf.Localization;
using Freeserf.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Freeserf.Test.Freeserf.Core
{
    [TestClass]
    public class GroundAnalysisTests
    {
        // Expected values follow the DOS routine at file offset 0x6E423:
        // skip center, sample rings 1..24 with weights 24..1, shift by 4, cap at 999.
        [TestMethod]
        [DataRow(0, 0u)]
        [DataRow(1, 46u)]
        [DataRow(2, 44u)]
        [DataRow(23, 3u)]
        [DataRow(24, 1u)]
        [DataRow(25, 0u)]
        public void SingleDeposit_ShouldUseOriginalRadiusAndWeights(int distance, uint expected)
        {
            var map = new Map(new MapGeometry(3), null);
            var generator = new FlatGrassMapGenerator(map);
            var center = map.Position(30, 30);
            var deposit = map.PositionAdd(center, distance, 0);
            generator.GetLandscape()[deposit].Mineral = Map.Minerals.Coal;
            generator.GetLandscape()[deposit].ResourceAmount = 31;
            map.InitTiles(generator);
            var game = new Game(map);
            var estimates = new uint[5];

            game.PrepareGroundAnalysis(center, estimates);

            Assert.AreEqual(expected, estimates[(int)Map.Minerals.Coal]);
        }

        [TestMethod]
        public void RichDeposits_ShouldCapEstimateAndNeverShowPerfectForCoal()
        {
            var map = new Map(new MapGeometry(3), null);
            var generator = new FlatGrassMapGenerator(map);
            foreach (var tile in generator.GetLandscape())
            {
                tile.Mineral = Map.Minerals.Coal;
                tile.ResourceAmount = 31;
            }
            map.InitTiles(generator);
            var game = new Game(map);
            var estimates = new uint[5];

            game.PrepareGroundAnalysis(map.Position(30, 30), estimates);

            Assert.AreEqual(999u, estimates[(int)Map.Minerals.Coal]);
            Assert.AreEqual(Strings.Popup_ResourceAmountAboveAverage,
                PopupBox.GetGroundAnalysisResourceText(Map.Minerals.Coal, estimates[(int)Map.Minerals.Coal]));
        }

        [TestMethod]
        public void ResourceLabels_ShouldUseOriginalScaling()
        {
            Assert.AreEqual(Strings.Popup_ResourceAmountPerfect,
                PopupBox.GetGroundAnalysisResourceText(Map.Minerals.Gold, 400));
            Assert.AreEqual(Strings.Popup_ResourceAmountPerfect,
                PopupBox.GetGroundAnalysisResourceText(Map.Minerals.Stone, 400));
            Assert.AreEqual(Strings.Popup_ResourceAmountPerfect,
                PopupBox.GetGroundAnalysisResourceText(Map.Minerals.Iron, 800));
            Assert.AreEqual(Strings.Popup_ResourceAmountAverage,
                PopupBox.GetGroundAnalysisResourceText(Map.Minerals.Coal, 799));
        }
    }
}

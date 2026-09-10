using NUnit.Framework;
using ThreeInARow.Presentation;

namespace ThreeInARow.Tests
{
    public sealed class ResponsiveLayoutPolicyTests
    {
        [TestCase(720f, 1280f, ResponsiveLayoutMode.Portrait)]
        [TestCase(720f, 1600f, ResponsiveLayoutMode.Portrait)]
        [TestCase(720f, 1000f, ResponsiveLayoutMode.Compact)]
        [TestCase(1280f, 720f, ResponsiveLayoutMode.Wide)]
        [TestCase(1920f, 1080f, ResponsiveLayoutMode.Wide)]
        public void ClassifiesSupportedViewports(float width, float height, ResponsiveLayoutMode expected)
        {
            Assert.That(ResponsiveLayoutPolicy.Measure(width, height).Mode, Is.EqualTo(expected));
        }

        [TestCase(672f, 1244f)]
        [TestCase(672f, 1564f)]
        [TestCase(640f, 960f)]
        [TestCase(804f, 1248f)]
        public void BoardAlwaysFitsTheAvailableWidth(float width, float height)
        {
            var side = ResponsiveLayoutPolicy.BoardSide(width, height);

            Assert.That(side, Is.GreaterThan(0f));
            Assert.That(side, Is.LessThanOrEqualTo(width));
            Assert.That(side, Is.LessThanOrEqualTo(ResponsiveLayoutPolicy.MaximumBoardSide));
        }

        [Test]
        public void ShortViewportKeepsBoardPlayableAndDelegatesOverflowToScrolling()
        {
            var side = ResponsiveLayoutPolicy.BoardSide(672f, 720f);

            Assert.That(side, Is.EqualTo(ResponsiveLayoutPolicy.MinimumComfortableBoardSide));
        }
    }
}

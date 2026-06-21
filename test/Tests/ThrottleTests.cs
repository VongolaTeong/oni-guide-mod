using NextStepGuide.Util;
using Xunit;

namespace NextStepGuide.Tests
{
    public class ThrottleTests
    {
        [Fact]
        public void FirstCall_IsAlwaysReady()
        {
            var t = new Throttle(3f);
            Assert.True(t.Ready(0f));
        }

        [Fact]
        public void Gates_WithinInterval_ThenReleases()
        {
            var t = new Throttle(3f);
            Assert.True(t.Ready(0f));
            Assert.False(t.Ready(1f));
            Assert.False(t.Ready(2.99f));
            Assert.True(t.Ready(3f));
            Assert.False(t.Ready(4f));
            Assert.True(t.Ready(6f));
        }

        [Fact]
        public void ZeroInterval_IsAlwaysReady()
        {
            var t = new Throttle(0f);
            Assert.True(t.Ready(0f));
            Assert.True(t.Ready(0f));
        }
    }
}

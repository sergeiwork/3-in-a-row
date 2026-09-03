using NUnit.Framework;

namespace ThreeInARow.Tests
{
    public sealed class AcceptanceTests
    {
        [Test]
        public void FoundationScenario_IsRepeatable()
        {
            Assert.That(FoundationScenarioHarness.AssertRepeatable(), Is.Not.Empty);
        }

        [Test]
        public void BoardScenario_IsRepeatableAndPlayable()
        {
            Assert.That(BoardScenarioHarness.AssertRepeatableAndPlayable(), Is.Not.Empty);
        }

        [Test]
        public void CombatScenario_IsRepeatableAndOrdered()
        {
            Assert.That(CombatScenarioHarness.AssertRepeatableAndOrdered(), Is.Not.Empty);
        }

        [Test]
        public void ProgressionScenario_IsRepeatableAndPersistent()
        {
            Assert.That(ProgressionScenarioHarness.AssertRepeatableAndPersistent(), Is.Not.Empty);
        }
    }
}

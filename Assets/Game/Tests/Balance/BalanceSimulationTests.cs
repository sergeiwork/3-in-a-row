using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace ThreeInARow.BalanceTests
{
    public sealed class BalanceSimulationTests
    {
        private const int RunsPerStrategy = 200;
        private const ulong FirstSeed = 0xBA1A0000UL;

        [TestCase(BalanceAffinity.Ember)]
        [TestCase(BalanceAffinity.Tide)]
        [TestCase(BalanceAffinity.Venom)]
        [TestCase(BalanceAffinity.Volt)]
        [TestCase(BalanceAffinity.Opportunist)]
        [Explicit("Expensive statistical suite: run manually on a local workstation only.")]
        public void FocusedStrategy_SimulatesFullRunsAndWritesBalanceReport(BalanceAffinity affinity)
        {
            var simulator = new BalanceRunSimulator(affinity);
            var runs = new List<BalanceRunResult>();
            var seedOffset = (ulong)affinity * 100000UL;
            for (var index = 0; index < RunsPerStrategy; index++)
                runs.Add(simulator.Run(FirstSeed + seedOffset + (ulong)index));

            var repeat = simulator.Run(runs[0].Seed);
            Assert.That(repeat.Signature(), Is.EqualTo(runs[0].Signature()),
                "The same seed and strategy must produce the same decisions and outcome.");
            Assert.That(runs, Has.Count.EqualTo(RunsPerStrategy));
            Assert.That(runs.All(run => run.MovesChosen > 0), Is.True,
                "Every simulated run should exercise board decisions.");
            Assert.That(runs.Sum(run => run.MovePreviews), Is.GreaterThan(runs.Sum(run => run.MovesChosen)),
                "The bot should compare alternatives instead of accepting the first legal move.");
            Assert.That(runs.Sum(run => run.RewardsChosen), Is.GreaterThan(0),
                "The sample should exercise upgrade selection.");
            Assert.That(runs.Sum(run => run.ActiveSkillsUsed), Is.GreaterThan(0),
                "The sample should exercise active-skill decisions.");
            Assert.That(runs.Any(run => !run.Victory), Is.True,
                "A strong heuristic strategy should not win every sampled run.");
            if (affinity != BalanceAffinity.Opportunist)
                Assert.That(runs.Sum(run => run.AffinitySkillsChosen), Is.GreaterThan(0),
                    "An elemental strategy should select upgrades from its intended branch.");

            var reportPath = BalanceReportWriter.Write(affinity, runs);
            TestContext.Progress.WriteLine(affinity + " balance report: " + reportPath);
        }
    }
}

using Xunit;

namespace Poker.Tests.Integration
{
	// Real servers on real sockets run one test class at a time, after the parallel tests. Run in parallel, several
	// in-process servers made connects and hellos time out on the first run after a build; run alone they never did.
	[CollectionDefinition(Name, DisableParallelization = true)]
	public sealed class IntegrationCollection
	{
		public const string Name = "Integration";
	}
}
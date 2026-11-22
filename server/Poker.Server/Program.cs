using Poker.Server.Infrastructure.Hosting;

namespace Poker.Server
{
	public static class Program
	{
		public static void Main(string[] args) => ServerHost.Build(args).Run();
	}
}
using System.Collections.Generic;
using System.Linq;

namespace DiningPhilosophers1
{
	public class Philosophers : List<Philosopher>
	{
		private readonly int _philosopherCount = ConfigValue.Inst.PhilosopherCount;
		private readonly int _forkCount = ConfigValue.Inst.ForkCount;

		public Philosophers InitializePhilosophers()
		{
			// Initialize the Forks
			// We need the forks because each philosopher needs to 
			// acquire both right and left forks in order to eat.
			//
			var forks = new List<Fork>();
			Enumerable.Range(0, _forkCount).ToList().ForEach(fName => forks.Add(new Fork(fName)));

			// Initialize the philosophers
			// Philosopher[i] needs 
			//		Fork[(i - 1) % 5] as her/his left fork
			//		Fork[i] as her/his right fork
			//
			int LeftForkName(int phName) => (_forkCount + phName - 1) % _forkCount;
			int RightForkName(int phName) => phName;
			Fork LeftFork(int phName) => forks[LeftForkName(phName)];
			Fork RightFork(int phName) => forks[RightForkName(phName)];

			// The Add(new Philosopher(..)) is Adding a philosopher to this class leveraging the List<Philosopher> base class...
			Enumerable.Range(0, _philosopherCount).ToList().ForEach(phName => Add(new Philosopher(phName, LeftFork(phName), RightFork(phName), this)));

			return this;
		}
	}
}

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
			// Comment: the function LeftForkName(..) calculate the modulu against the fork-count by first
			//			adding fork-count: (_forkCount + phName - 1) % _forkCount
			//			This is done because the c# modulo operator goes from negative (remainder - 1) to 
			//			pesitive (reminder - 1).  So: -1 % 5 is -1 and not 4.  Therefore, to avoid negative 
			//			numbers we add the reminder.  So: (5 - 1) % 5 is the expected 4.
			//
			// Digression:
			//			In general if we have three arbitrary numbers a, b and m then to avoid negative
			//			numbers in the result (a - b) % m will use:
			//				(m + (a - b) % m) % m
			//			The internal experession: 
			//				(a - b) % m
			//			will yield result in the range (-m, m).  (Including neither -m nor m as part 
			//			of the range).  So the entire expression:
			//				(m + (a - b) % m) % m
			//			will yield result in the range of [0, m) (including 0 and excluding m).
			//
			//			Modulo artithmatic is not something we do frequently.  If you still need to
			//			convince yourself of the validity of the above, then pin down m say 5 (as it
			//			is in our case) so to work out (5 + (a - b) % 5) % 5.
			//			Calculate the results starting with:
			//				(a - b) % 5.
			//			Which yields a number in the range of (-4, -3, -2, -1, 0, 1, 2, 3, 4).
			//			So (5 + (a - b) % 5) % 5 is reduced to:
			//			(5 + a number in the range of (-4, -3, -2, -1, 0, 1, 2, 3, 4)) % 5
			//			Add 5.
			//			Now you get (a number in the range of (1, 2, 3, 4, 5, 6, 7, 8, 9)) % 5
			//			Take the modulo with respect to 5
			//			And now you get (a number in (1, 2, 3, 4, 0, 1, 2, 3, 4))
			//			which is a number in the range [0, 5).
			//
			int LeftForkName(int phName) => (_forkCount + phName - 1) % _forkCount;
			int RightForkName(int phName) => phName;
			Fork LeftFork(int phName) => forks[LeftForkName(phName)];
			Fork RightFork(int phName) => forks[RightForkName(phName)];

			// The Add(new Philosopher(..)) is Adding a philosopher to this class leveraging the List<Philosopher> base class...
			Enumerable.Range(0, _philosopherCount).ToList().ForEach(phName => Add(new Philosopher(phName, LeftFork(phName), RightFork(phName), this)));

			// There is no need to expose the forks independently.  They will be used only as they relate to the philosophers and as such they
			// will be accessed through the philosopher instances only.
			return this;
		}
	}
}

using System;

namespace DiningPhilosophers1
{
	public class Fork
	{
		public Fork(int name)
		{
			Name = name;
			_isInUse = false;
			_whichPhilosopher = null;
		}

		/// <summary>Name of fork</summary>
		public int Name { get; }

		private bool _isInUse;

		/// <summary>
		/// Is fork in use
		/// Also ensure that if fork is not in use it cannot be reset to not-in-use
		/// and if it is in use then it cannot be set to be used by another philosopher
		/// </summary>
		public bool IsInUse
		{
			get => _isInUse;
			set
			{
				if (!(value ^ _isInUse))
				{
					var msg = value
						? $"Cannot assign fork F{Name} when it is already assigned"
						: $"Cannot free fork F{Name} when it is already free";
					throw new Exception(msg);
				}
				_isInUse = value;
			}
		}

		private Philosopher _whichPhilosopher;

		/// <summary>
		/// If the fork is in use then keep the philosopher using it.
		/// Also ensure that if no philosopher is using the fork then it is a
		/// mistake to reassign it to no-philosopher and converse if a philosopher
		/// is using the fork then no other philosopher can use that fork.
		/// </summary>
		public Philosopher WhichPhilosopher
		{
			get => _whichPhilosopher;
			set
			{
				if (!((value == null) ^ (_whichPhilosopher == null)))
				{
					var msg = value == null
						? $"Cannot assign fork F{Name} to no-Philosopher when it is already free"
						: $"Cannot assign fork F{Name} to philosopher Ph{value} while it is assigned to philosopher Ph{_whichPhilosopher}";
					throw new Exception(msg);
				}
				_whichPhilosopher = value;
			}
		}
	}
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DiningPhilosophers1
{
	public class Philosopher
	{
		public Philosopher(int name, Fork leftFork, Fork rightFork, Philosophers allPhilosophers)
		{
			Name = name;
			LeftFork = leftFork;
			RightFork = rightFork;
			_allPhilosophers = allPhilosophers;

			_rnd = new Random(Name); // Randomize eating time with a different seed for each philosopher
		}

		/// <summary>ConfigValue.Inst.MaxPhilsophersToEatSimultaneously, 2, philosophers may eat at the same time</summary>
		static readonly SemaphoreSlim AquireEatPermissionSlip = new SemaphoreSlim(ConfigValue.Inst.MaxPhilsophersToEatSimultaneously);

		/// <summary>Synchronization object for acquiring forks and change status.</summary>
		private static readonly object Sync = new object();

		/// <summary>
		/// Name of philosopher
		/// As you can see the philosophers are OK with a numeric name.  They are good that way.
		/// </summary>
		public int Name { get; }

		/// <summary>Track philosopher left fork</summary>
		private Fork LeftFork { get; }

		/// <summary>Track philosopher right fork</summary>
		private Fork RightFork { get; }

		public PhilosopherStatus Status { get; private set; } = PhilosopherStatus.Thinking;

		/// <summary>
		/// Statistical information
		/// How many times this philosopher was given a go ahead to eat
		/// </summary>
		public int EatCount { get; private set; }

		/// <summary>
		/// Statistical information
		/// Total duration of eating in milliseconds
		/// </summary>
		public int TotalEatingTime { get; private set; }

		/// <summary>
		/// Statistical information
		/// Total Conflict count.  A philosopher was given an eating slip but an ajacent philosopher was already eating.
		/// </summary>
		public int ConflictCount { get; private set; }

		/// <summary>A philosopher may inquire about other dining philosophers</summary>
		private readonly Philosophers _allPhilosophers;

		/// <summary>Regulates the duration of eating</summary>
		private readonly Random _rnd;

		/// <summary>Retrieve some values once</summary>
		private readonly int _maxThinkDuration = ConfigValue.Inst.MaxThinkDuration;
		private readonly int _minThinkDuration = ConfigValue.Inst.MinThinkDuration;

		/// <summary>
		/// The routine each Task employs for the eating philosophers
		/// </summary>
		/// <param name="stopDining"></param>
		public void EatingHabit(CancellationToken stopDining)
		{
			// After eat permission was granted.  The philosopher will wait for a
			// duration of durationBeforeRequstEatPermission before asking for an
			// eat permission again.
			var durationBeforeRequstEatPermission = ConfigValue.Inst.DurationBeforeAskingPermissionToEat;

			for (var i = 0;; ++i)
			{
				// If calling routine is asking for dining to stop then comply and stop.
				if (stopDining.IsCancellationRequested)
				{
					Console.WriteLine($"{DateTime.Now:HH:mm:ss.ffff}             Ph{Name} IS ASKED TO STOP THE DINING EXPERIENCE");
					stopDining.ThrowIfCancellationRequested();
				}

				try
				{
					// Ask for eating permission.
					// Note: Once eating permission is granted, the philosopher still needs to 
					// check if the left and right forks are available
					AquireEatPermissionSlip.Wait();

					Console.WriteLine($"{DateTime.Now:HH:mm:ss.ffff} Philosopher Ph{Name} will attempt to eat.  Attempt: {i}.");

					bool isOkToEat;
					lock (Sync)
					{
						// Check for Fork availability
						isOkToEat = IsForksAvailable();
						if (isOkToEat)
							AquireForks();                      // May throw an exception
					}

					if (isOkToEat)
					{
						PhilosopherEat();
						ReleaseForks();                         // May throw an exception
					}
					else
						++ConflictCount;
				}
				catch (Exception ex)
				{
					Console.WriteLine($"{DateTime.Now:HH:mm:ss.ffff} ERORR...    Ph{Name} ENCOUNTER AN ERROR: {ex.Message} " +
									  $"AND NOW IS NOT PARTICIPATING IN THE DINING EXPERIENCE {new string('.', 20)}");
					throw;
				}
				finally
				{
					AquireEatPermissionSlip.Release();
				}

				// Wait for a duration of durationBeforeRequstEatPermission
				// before asking for eat permission.
				Thread.Sleep(durationBeforeRequstEatPermission);
			}
		}

		private bool IsForksAvailable()
		{
			// Note that this Sync is superfluous because to be effective
			// the entire method should be called under a lock (sync).
			// Otherwise, after the lock when the method checks for fork
			// availability and before the return, one or both forks may
			// be snatched by another philosopher.
			lock (Sync)
			{
				if (LeftFork.IsInUse)
				{
					Console.WriteLine($"{DateTime.Now:HH:mm:ss.ffff} Philosopher Ph{Name} cannot eat " +
					                  $"because her/his Left Fork, F{LeftFork.Name}, is in use (by philosopher Ph{LeftFork.WhichPhilosopher.Name})");
					return false;
				}

				if (RightFork.IsInUse)
				{
					Console.WriteLine($"{DateTime.Now:HH:mm:ss.ffff} Philosopher Ph{Name} cannot eat " +
					                  $"because her/his right Fork, F{RightFork.Name}, is in use (by philosopher Ph{RightFork.WhichPhilosopher.Name})");
					return false;
				}
			}

			// Both forks are available, philosopher may commence eating
			return true;
		}

		private void PhilosopherEat()
		{
			// Eating duration is randomized
			var eatingDuration = _rnd.Next(_maxThinkDuration) + _minThinkDuration;

			// Display who is eating
			var eatingPhilosophers = EatingPhilosphers().Select(p => p.Name).ToList();
			Console.WriteLine($"{DateTime.Now:HH:mm:ss.ffff} Philosopher Ph{Name} is eating.  " +
			                  $"There are: {eatingPhilosophers.Count} " +
			                  $"({string.Join(", ", eatingPhilosophers.Select(p => $"Ph{p}"))}) eating philosophers.");

			//
			// Simulate our philosopher eating yummy spaghetti
			//
			Thread.Sleep(eatingDuration);

			Console.WriteLine($"{DateTime.Now:HH:mm:ss.ffff} Philosopher Ph{Name} is thinking after eating yummy spaghetti for {eatingDuration} milliseconds.");

			// Statistics update
			++EatCount;
			TotalEatingTime += eatingDuration;
		}

		private IEnumerable<Philosopher> EatingPhilosphers()
		{
			lock (Sync)
				return _allPhilosophers.Where(p => p.Status == PhilosopherStatus.Eating);
		}

		private void AquireForks()
		{
			lock (Sync)
			{
				LeftFork.IsInUse = true;
				LeftFork.WhichPhilosopher = this;
				RightFork.IsInUse = true;
				RightFork.WhichPhilosopher = this;

				Status = PhilosopherStatus.Eating;
				Console.WriteLine($"{DateTime.Now:HH:mm:ss.ffff} Philosopher Ph{Name} acquired forks: (F{LeftFork.Name}, F{RightFork.Name}).");
			}
		}

		void ReleaseForks()
		{
			lock (Sync)
			{
				LeftFork.IsInUse = false;
				LeftFork.WhichPhilosopher = null;
				RightFork.IsInUse = false;
				RightFork.WhichPhilosopher = null;

				Status = PhilosopherStatus.Thinking;
				Console.WriteLine($"{DateTime.Now:HH:mm:ss.ffff} Philosopher Ph{Name} released forks: (F{LeftFork.Name}, F{RightFork.Name}).");
			}
		}
	}
}
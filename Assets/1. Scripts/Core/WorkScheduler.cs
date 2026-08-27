using System.Collections.Generic;

namespace Churub.Core
{
    public sealed class WorkScheduler<T> where T : class, IWorkTarget
    {
        private readonly List<T> targets = new List<T>();
        private readonly HashSet<T> reservations = new HashSet<T>();

        public int TargetCount => targets.Count;
        public int ReservationCount => reservations.Count;

        public void Register(T target)
        {
            if (target != null && !targets.Contains(target))
            {
                targets.Add(target);
            }
        }

        public void Unregister(T target)
        {
            if (target == null)
            {
                return;
            }

            targets.Remove(target);
            reservations.Remove(target);
        }

        public bool TryReserveBest(out T target)
        {
            target = null;
            var highestStackCount = 0;

            foreach (var candidate in targets)
            {
                if (candidate == null || reservations.Contains(candidate))
                {
                    continue;
                }

                var stackCount = candidate.GetStackCount();
                if (stackCount > highestStackCount)
                {
                    highestStackCount = stackCount;
                    target = candidate;
                }
            }

            if (target == null)
            {
                return false;
            }

            reservations.Add(target);
            return true;
        }

        public void Release(T target)
        {
            if (target != null)
            {
                reservations.Remove(target);
            }
        }

        public bool IsReserved(T target)
        {
            return target != null && reservations.Contains(target);
        }
    }
}

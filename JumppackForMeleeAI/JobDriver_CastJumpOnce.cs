using RimWorld;
using System.Linq;
using Verse;
using Verse.AI;

namespace JumppackForMeleeAI {
    public class JobDriver_CastJumpOnce : JobDriver_CastVerbOnce {
        public override bool TryMakePreToilReservations(bool errorOnFailed) {
            if (pawn.jobs.AllJobs().Count(t => t.def == JumpJobDefOf.CastJumpOnce) > 1) {
                return false;
            }
            return true;
        }
    }
    [DefOf]
    public static class JumpJobDefOf {
        static JumpJobDefOf() {
            DefOfHelper.EnsureInitializedInCtor(typeof(JobDefOf));
        }
        public static JobDef CastJumpOnce;
    }
}

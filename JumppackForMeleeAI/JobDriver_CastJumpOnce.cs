using RimWorld;
using System.Linq;
using Verse;
using Verse.AI;

namespace JumppackForMeleeAI {
    public class JobDriver_CastJumpOnce : JobDriver_CastVerbOnce {
        public override bool TryMakePreToilReservations(bool errorOnFailed) {
            var target = pawn.mindState.enemyTarget;
            if (target != null && target.Position.DistanceToSquared(pawn.Position) < 10) {
                return false;
            }
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

using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using static UnityEngine.GraphicsBuffer;

namespace JumppackForMeleeAI {
    internal class JobGiver_AIMeleeJumppack : ThinkNode_JobGiver {
        public override ThinkNode DeepCopy(bool resolve = true) {
            JobGiver_AIMeleeJumppack jobGiver_AIMeleeJumppack = (JobGiver_AIMeleeJumppack)base.DeepCopy(resolve);
            jobGiver_AIMeleeJumppack.minTargetDistance = this.minTargetDistance;
            return jobGiver_AIMeleeJumppack;
        }
        protected override Job TryGiveJob(Pawn pawn) {
            if (!pawn.RaceProps.Humanlike || pawn.IsColonist) {
                return null;
            }

            List<IAttackTarget> enemyTargets = pawn.Map.attackTargetsCache.GetPotentialTargetsFor(pawn);
            IAttackTarget firstTarget = enemyTargets.First(t => t.Thing is Pawn);
            if (enemyTargets.NullOrEmpty() || firstTarget != null) {
                if (DebugSettings.godMode) {
                    MoteMaker.ThrowText(pawn.DrawPosHeld ?? pawn.PositionHeld.ToVector3Shifted(), pawn.MapHeld,
                        "[jumppack]no target");
                }
                return null;
            }
            Pawn targetPawn = firstTarget as Pawn;
            Verb attackVerb = pawn.TryGetAttackVerb(enemyTargets.First().Thing, false, true);
            if (attackVerb == null || !attackVerb.verbProps.IsMeleeAttack) {
                if (DebugSettings.godMode) {
                    MoteMaker.ThrowText(pawn.DrawPosHeld ?? pawn.PositionHeld.ToVector3Shifted(), pawn.MapHeld,
                        "[jumppack]ranged");
                }
                return null;
            }
            if (pawn.CanReachImmediate(targetPawn, PathEndMode.Touch)) {
                if (DebugSettings.godMode) {
                    MoteMaker.ThrowText(pawn.DrawPosHeld ?? pawn.PositionHeld.ToVector3Shifted(), pawn.MapHeld,
                        "[jumppack]reached, not required");
                }
                return null;
            }

            if ((float)(pawn.Position - targetPawn.Position).LengthHorizontalSquared < minTargetDistance) {
                if (DebugSettings.godMode) {
                    MoteMaker.ThrowText(pawn.DrawPosHeld ?? pawn.PositionHeld.ToVector3Shifted(), pawn.MapHeld,
                        "[jumppack]too close (distance:" + (float)(pawn.Position - targetPawn.Position).LengthHorizontalSquared + ")");
                }
                return null;
            }
            
            if(targetPawn.pather.Moving) {
                if (DebugSettings.godMode) {
                    MoteMaker.ThrowText(pawn.DrawPosHeld ?? pawn.PositionHeld.ToVector3Shifted(), pawn.MapHeld,
                        "[jumppack]target is moving");
                }
                return null;
            }
            
            Verb jumpVerb = TryGetJumpVerb(pawn, targetPawn);
            if (jumpVerb == null) {
                if (DebugSettings.godMode) {
                    MoteMaker.ThrowText(pawn.DrawPosHeld ?? pawn.PositionHeld.ToVector3Shifted(), pawn.MapHeld,
                        "[jumppack]no jump verb");
                }
                return null;
            }

            if (DebugSettings.godMode) {
                MoteMaker.ThrowText(pawn.DrawPosHeld ?? pawn.PositionHeld.ToVector3Shifted(), pawn.MapHeld,
                        "[jumppack]distance: " + (float)(pawn.Position - targetPawn.Position).LengthHorizontalSquared);
            }
            Job job = JobMaker.MakeJob(JumpJobDefOf.CastJumpOnce, targetPawn);
            job.verbToUse = jumpVerb;
            return job;
        }

        static public Verb TryGetJumpVerb(Pawn pawn,LocalTargetInfo target) {
            IEnumerable<Verb> jumpVerbs =
                pawn.VerbTracker.AllVerbs
                .Concat(pawn.equipment.AllEquipmentVerbs)
                .Concat(pawn.apparel.AllApparelVerbs)
                .Where(t => t is Verb_Jump);
            if (jumpVerbs.EnumerableNullOrEmpty<Verb>()) {
                return null;
            }

            return jumpVerbs.FirstOrDefault(t => t.IsStillUsableBy(pawn) && t.CanHitTarget(target));
        }



        private float minTargetDistance = 25f;
    }
}

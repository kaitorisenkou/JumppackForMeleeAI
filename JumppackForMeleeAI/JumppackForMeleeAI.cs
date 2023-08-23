using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;
using RimWorld;
using HarmonyLib;
using System.Text.RegularExpressions;
using Verse.AI;
using static UnityEngine.GraphicsBuffer;

namespace JumppackForMeleeAI {
    [StaticConstructorOnStartup]
    public class JumppackForMeleeAI {
        static JumppackForMeleeAI() {
            
            Log.Message("[JumppackForMeleeAI]Now Active");
            var harmony = new Harmony("kaitorisenkou.JumppackForMeleeAI");
            //ManualPatch(harmony);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Log.Message("[JumppackForMeleeAI]Harmony patch complete!");
            
        }
        /*
        static void ManualPatch(Harmony harmony) {
            Type FollowAndMeleeAttack_innerType = AccessTools.TypeByName("Verse.AI.Toils_Combat+<>c__DisplayClass6_0");
            if (FollowAndMeleeAttack_innerType != null) {
                MethodInfo m = AccessTools.Method(FollowAndMeleeAttack_innerType, "<FollowAndMeleeAttack>b__0");
                if (m != null) {
                    harmony.Patch(m, null, null,
                        new HarmonyMethod(AccessTools.Method(typeof(FollowAndMeleeAttack_Patch), "Transpiler", new Type[] { typeof(IEnumerable<CodeInstruction>), typeof(ILGenerator) })));
                    Log.Message("[JumppackForMeleeAI]Patched: <FollowAndMeleeAttack>b__0");
                }
            }
        }
        */
    }
    [HarmonyPatch(typeof(JobGiver_AIFightEnemy), "TryGiveJob")]
    public static class Patch_JobGiver_AIFightEnemy {
        public static int patchCount = 0;
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
            var instructionList = instructions.ToList();
            MethodInfo methodInfo_IsMelee = AccessTools.Method(typeof(VerbProperties),"get_IsMeleeAttack");
            MethodInfo methodInfo_ShootPos = AccessTools.Method(typeof(JobGiver_AIFightEnemy), "TryFindShootingPosition");
            MethodInfo methodInfo_Makejob = AccessTools.Method(typeof(JobMaker), nameof(JobMaker.MakeJob));
            for (int i = 0; i < instructionList.Count; i++) {
                if (patchCount == 0) {
                    if (instructionList[i].opcode == OpCodes.Callvirt &&
                        (MethodInfo)instructionList[i].operand == methodInfo_IsMelee) {
                        Label label1 = generator.DefineLabel();
                        CodeInstruction popCode1 = new CodeInstruction(OpCodes.Pop);
                        popCode1.labels.Add(label1);
                        instructionList.InsertRange(i + 2, new CodeInstruction[] {
                            new CodeInstruction(OpCodes.Ldarg_1),//2
                            new CodeInstruction(OpCodes.Call,AccessTools.Method(typeof(Patch_JobGiver_AIFightEnemy),nameof(GetJunpPackMelee))),//3
                            new CodeInstruction(OpCodes.Dup),//4
                            new CodeInstruction(OpCodes.Brfalse_S,label1),//5
                            new CodeInstruction(OpCodes.Ret),//6
                            popCode1//7
                        });
                        patchCount++;
                        i += 13;
                        Label label2 = generator.DefineLabel();
                        CodeInstruction popCode = new CodeInstruction(OpCodes.Pop);
                        popCode.labels.Add(label2);
                        instructionList.InsertRange(i, new CodeInstruction[] {
                            new CodeInstruction(OpCodes.Call,AccessTools.Method(typeof(Patch_JobGiver_AIFightEnemy),nameof(GetJunpPackRanged))),
                            new CodeInstruction(OpCodes.Dup),
                            new CodeInstruction(OpCodes.Brfalse_S,label2),
                            new CodeInstruction(OpCodes.Ret),
                            popCode,
                            new CodeInstruction(OpCodes.Ldarg_1)
                        });
                        patchCount++;
                        break;
                    }
                }
            }
            if (patchCount < 2) {
                Log.Warning("[JumppackForMeleeAI]Patch_JobGiver_AIFightEnemy failed!");
            }
            return instructionList;
        }

        public static Job GetJunpPackMelee(Pawn pawn) {
            if (!pawn.RaceProps.Humanlike || pawn.IsColonist) {
                return null;
            }

            Thing targetPawn = pawn.mindState.enemyTarget;
            if (pawn.CanReachImmediate(targetPawn, PathEndMode.Touch)) {
#if DEBUG
                if (DebugSettings.godMode) {
                    MoteMaker.ThrowText(pawn.DrawPosHeld ?? pawn.PositionHeld.ToVector3Shifted(), pawn.MapHeld,
                        "[jumppack-melee]reached, not required");
                }
#endif
                return null;
            }

            if ((float)(pawn.Position - targetPawn.Position).LengthHorizontalSquared < 16) {
#if DEBUG
                if (DebugSettings.godMode) {
                    MoteMaker.ThrowText(pawn.DrawPosHeld ?? pawn.PositionHeld.ToVector3Shifted(), pawn.MapHeld,
                        "[jumppack-melee]too close (distance:" + (float)(pawn.Position - targetPawn.Position).LengthHorizontalSquared + ")");
                }
#endif
                return null;
            }
            /*
            if (targetPawn is Pawn && ((Pawn)targetPawn).pather.Moving) {
#if DEBUG
                if (DebugSettings.godMode) {
                    MoteMaker.ThrowText(pawn.DrawPosHeld ?? pawn.PositionHeld.ToVector3Shifted(), pawn.MapHeld,
                        "[jumppack-melee]target is moving");
                }
#endif
                return null;
            }
            */
            Verb jumpVerb = TryGetJumpVerb(pawn, targetPawn);
            if (jumpVerb == null) {
#if DEBUG
                if (DebugSettings.godMode) {
                    MoteMaker.ThrowText(pawn.DrawPosHeld ?? pawn.PositionHeld.ToVector3Shifted(), pawn.MapHeld,
                        "[jumppack-melee]no jump verb");
                }
#endif
                return null;
            }
#if DEBUG
            if (DebugSettings.godMode) {
                MoteMaker.ThrowText(pawn.DrawPosHeld ?? pawn.PositionHeld.ToVector3Shifted(), pawn.MapHeld,
                        "[jumppack-melee]distance: " + (float)(pawn.Position - targetPawn.Position).LengthHorizontalSquared);
            }
#endif
            Job job = JobMaker.MakeJob(JumpJobDefOf.CastJumpOnce, targetPawn);
            job.verbToUse = jumpVerb;
            return job;
        }


        public static Job GetJunpPackRanged(Pawn pawn) {
            if (!pawn.RaceProps.Humanlike || pawn.IsColonist) {
                return null;
            }

            Thing targetPawn = pawn.mindState.enemyTarget;

            var covers = CoverUtility.CalculateCoverGiverSet(targetPawn, pawn.Position, pawn.Map);
            if (covers.NullOrEmpty() || covers.All(t => t.BlockChance < 0.3f)) {
#if DEBUG
                if (DebugSettings.godMode) {
                    MoteMaker.ThrowText(pawn.DrawPosHeld ?? pawn.PositionHeld.ToVector3Shifted(), pawn.MapHeld,
                        "[jumppack-ranged]no covers, not required");
                }
#endif
                return null;
            }

            IntVec3 opposide = targetPawn.Position + pawn.Rotation.FacingCell * 3;
            Verb jumpVerbOpposide = TryGetJumpVerb(pawn, opposide);
            if (jumpVerbOpposide != null) {
#if DEBUG
            if (DebugSettings.godMode) {
                MoteMaker.ThrowText(pawn.DrawPosHeld ?? pawn.PositionHeld.ToVector3Shifted(), pawn.MapHeld,
                        "[jumppack-ranged]distance: " + (float)(pawn.Position - targetPawn.Position).LengthHorizontalSquared);
            }
#endif
                Job jobOpposide = JobMaker.MakeJob(JumpJobDefOf.CastJumpOnce, opposide);
                jobOpposide.verbToUse = jumpVerbOpposide;
                return jobOpposide;
            }
            IntVec3 coverFront = targetPawn.Position - pawn.Rotation.FacingCell;
            Verb jumpVerbCoverFront = TryGetJumpVerb(pawn, coverFront);
            if (jumpVerbCoverFront==null) {
#if DEBUG
                if (DebugSettings.godMode) {
                    MoteMaker.ThrowText(pawn.DrawPosHeld ?? pawn.PositionHeld.ToVector3Shifted(), pawn.MapHeld,
                        "[jumppack-ranged]no jump verb");
                }
#endif
                return null;
            }
            Job jobCoverFront = JobMaker.MakeJob(JumpJobDefOf.CastJumpOnce, coverFront);
            jobCoverFront.verbToUse = jumpVerbCoverFront;
            return jobCoverFront;
        }

        static public Verb TryGetJumpVerb(Pawn pawn, LocalTargetInfo target) {
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
    }
    
}

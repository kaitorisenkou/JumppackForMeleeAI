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
            MethodInfo methodInfo = AccessTools.Method(typeof(VerbProperties),"get_IsMeleeAttack");
            for (int i = 0; i < instructionList.Count; i++) {
                if (instructionList[i].opcode == OpCodes.Callvirt && (MethodInfo)instructionList[i].operand == methodInfo) {
                    Label label = generator.DefineLabel();
                    instructionList.InsertRange(i +2, new CodeInstruction[] {
                        new CodeInstruction(OpCodes.Ldarg_1),
                        new CodeInstruction(OpCodes.Call,AccessTools.Method(typeof(Patch_JobGiver_AIFightEnemy),nameof(GetJunpPack))),
                        new CodeInstruction(OpCodes.Dup),
                        new CodeInstruction(OpCodes.Brfalse_S,label),
                        new CodeInstruction(OpCodes.Ret),
                        new CodeInstruction(OpCodes.Pop)
                    });
                    instructionList[i + 7].labels.Add(label);
                    patchCount++;
                    break;
                }
            }
            return instructionList;
        }

        public static Job GetJunpPack(Pawn pawn) {
            if (!pawn.RaceProps.Humanlike || pawn.IsColonist) {
                return null;
            }

            Thing targetPawn = pawn.mindState.enemyTarget;
            if (pawn.CanReachImmediate(targetPawn, PathEndMode.Touch)) {
#if DEBUG
                if (DebugSettings.godMode) {
                    MoteMaker.ThrowText(pawn.DrawPosHeld ?? pawn.PositionHeld.ToVector3Shifted(), pawn.MapHeld,
                        "[jumppack]reached, not required");
                }
#endif
                return null;
            }

            if ((float)(pawn.Position - targetPawn.Position).LengthHorizontalSquared < 16) {
#if DEBUG
                if (DebugSettings.godMode) {
                    MoteMaker.ThrowText(pawn.DrawPosHeld ?? pawn.PositionHeld.ToVector3Shifted(), pawn.MapHeld,
                        "[jumppack]too close (distance:" + (float)(pawn.Position - targetPawn.Position).LengthHorizontalSquared + ")");
                }
#endif
                return null;
            }
            /*
            if (targetPawn is Pawn && ((Pawn)targetPawn).pather.Moving) {
#if DEBUG
                if (DebugSettings.godMode) {
                    MoteMaker.ThrowText(pawn.DrawPosHeld ?? pawn.PositionHeld.ToVector3Shifted(), pawn.MapHeld,
                        "[jumppack]target is moving");
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
                        "[jumppack]no jump verb");
                }
#endif
                return null;
            }
#if DEBUG
            if (DebugSettings.godMode) {
                MoteMaker.ThrowText(pawn.DrawPosHeld ?? pawn.PositionHeld.ToVector3Shifted(), pawn.MapHeld,
                        "[jumppack]distance: " + (float)(pawn.Position - targetPawn.Position).LengthHorizontalSquared);
            }
#endif
            Job job = JobMaker.MakeJob(JumpJobDefOf.CastJumpOnce, targetPawn);
            job.verbToUse = jumpVerb;
            return job;
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

using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using Quintessential;
using Quintessential.Settings;
using SDL2;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace FrameratePatch;

//using PartType = class_139;
//using Permissions = enum_149;
//using BondType = enum_126;
//using BondSite = class_222;
//using Bond = class_277;
//using AtomTypes = class_175;
//using PartTypes = class_191;
//using Texture = class_256;
public class MainClass : QuintessentialMod
{
	/*
	private static bool saveFramerate = true;
	public override Type SettingsType => typeof(MySettings);
	public class MySettings
	{
		[SettingsLabel("Recompile the instruction tape only when required.")]
		public bool enable = true;
	}
	public override void ApplySettings()
	{
		base.ApplySettings();
		saveFramerate = ((MySettings)Settings).enable;
	}
	*/

	//public static MethodInfo PrivateMethod<T>(string method) => typeof(T).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

	public override void Load()
	{
		//Settings = new MySettings();
	}
	public override void LoadPuzzleContent()
	{
		//
	}
	public override void Unload()
	{
		//
	}
	public override void PostLoad()
	{
		On.CompiledProgramGrid.method_855 += CompileTapeOnlyWhenNeeded;
		On.Solution.method_1965 += RequestTapeRecompile;

		IL.SolutionEditorProgramPanel.method_221 += ModifyProgramPanel_method_221;
	}

	////////////////////////////////////////////////////////////////////////////////////////////////////
	////////////////////////////////////////////////////////////////////////////////////////////////////
	////////////////////////////////////////////////////////////////////////////////////////////////////
	// improve framerate by: only compiling the instruction tape when necessary

	private static string storedID = "";
	private static Maybe<CompiledProgramGrid> storedCPG;
	private static bool updateStoredCPG = true;

	private static void RequestTapeRecompile(On.Solution.orig_method_1965 orig, Solution solution_self)
	{
		orig(solution_self);
		updateStoredCPG = true;
	}

	private static string get_CPG_ID(Solution solution)
	{
		string solutionID = solution.field_3915;
		Puzzle puzzle = solution.method_1934();
		string puzzleID = puzzle.field_2766;
		return solutionID + puzzleID;
	}

	private static Maybe<CompiledProgramGrid> CompileTapeOnlyWhenNeeded(On.CompiledProgramGrid.orig_method_855 orig, Solution solution)
	{
		//if (!saveFramerate) return orig(solution);

		if (updateStoredCPG || storedID != get_CPG_ID(solution))
		{
			storedID = get_CPG_ID(solution);
			updateStoredCPG = false;
			storedCPG = orig(solution);
		}

		return storedCPG;
	}

	////////////////////////////////////////////////////////////////////////////////////////////////////
	////////////////////////////////////////////////////////////////////////////////////////////////////
	// improve framerate by: while sim is running, only drawing the instructions that can be seen on-screen

	private static int ForLoopIteratorStart_Parts(SolutionEditorProgramPanel sepp) => ForLoopIteratorStart(sepp, true);
	private static int ForLoopIteratorStart_Instructions(SolutionEditorProgramPanel sepp) => ForLoopIteratorStart(sepp, false);

	private static int ForLoopIteratorStart(SolutionEditorProgramPanel sepp, bool getYBounds)
	{
		var sepp_dyn = new DynamicData(sepp);

		Vector2 field3988 = sepp_dyn.Get<Vector2>("field_3988");
		Index2 offset = new Index2(-(int)field3988.X, (int)field3988.Y);

		Vector2 field3982 = SolutionEditorProgramPanel.field_3982; // dimensions of an instruction tile
		Index2 dimensions = new Index2((int)field3982.X, (int)field3982.Y);

		if (getYBounds)
		{
			return offset.Y / dimensions.Y;
		}
		else
		{
			return offset.X / dimensions.X;
		}
	}

	private static void ModifyProgramPanel_method_221(ILContext il)
	{
		ILCursor cursor = new ILCursor(il);

		//////////////////////////////////////////////////
		//////////////////////////////////////////////////
		//////////////////////////////////////////////////
		////////// WHEN SIM IS RUNNING

		//////////////////////////////
		// go to roughly where the for-loop conditional occurs for rows
		cursor.Goto(640);
		// jump to just before the branch
		if (!cursor.TryGotoNext(MoveType.Before, instr => instr.Match(OpCodes.Blt))) return;

		// load the SolutionEditorProgramPanel self onto the stack so we can use it
		cursor.Emit(OpCodes.Ldarg_0);

		// change the comparand value
		cursor.EmitDelegate<Func<int, SolutionEditorProgramPanel, int>>((programHeight, sepp_self) =>
		{
			int ylimit = ForLoopIteratorStart_Parts(sepp_self);
			return Math.Min(ylimit + 7, programHeight);
		});


		//////////////////////////////
		// go to roughly where the for-loop conditional occurs for columns
		cursor.Goto(612);
		// jump to just before the branch
		if (!cursor.TryGotoNext(MoveType.Before, instr => instr.Match(OpCodes.Blt))) return;

		// load the SolutionEditorProgramPanel self and the part's CompiledProgram onto the stack so we can use it
		cursor.Emit(OpCodes.Ldarg_0);
		cursor.Emit(OpCodes.Ldloc_S, (byte)39);

		// change the comparand value
		cursor.EmitDelegate<Func<int, SolutionEditorProgramPanel, CompiledProgram, int>>((programWidth, sepp_self, program) =>
		{
			int programDelay = program.field_2366;
			int xlimit = ForLoopIteratorStart_Instructions(sepp_self) - programDelay;
			return Math.Min(xlimit + 50, programWidth);
		});


		//////////////////////////////
		// go to roughly where the for-loop is initialized for columns
		cursor.Goto(555);
		// jump to just before the branch
		if (!cursor.TryGotoNext(MoveType.Before, instr => instr.Match(OpCodes.Stloc_S))) return;

		// load the SolutionEditorProgramPanel self and the part's CompiledProgram onto the stack so we can use it
		cursor.Emit(OpCodes.Ldarg_0);
		cursor.Emit(OpCodes.Ldloc_S, (byte) 39);

		// change the initial value
		cursor.EmitDelegate<Func<int, SolutionEditorProgramPanel, CompiledProgram, int>>((zero, sepp_self, program) =>
		{
			int programDelay = program.field_2366;
			int xlimit = ForLoopIteratorStart_Instructions(sepp_self) - programDelay;
			return Math.Max(xlimit, 0);
		});


		//////////////////////////////
		// go to roughly where the for-loop is initialized for rows
		cursor.Goto(537);
		// jump to just before the branch
		if (!cursor.TryGotoNext(MoveType.Before, instr => instr.Match(OpCodes.Stloc_S))) return;

		// load the SolutionEditorProgramPanel self onto the stack so we can use it
		cursor.Emit(OpCodes.Ldarg_0);

		// change the initial value
		cursor.EmitDelegate<Func<int, SolutionEditorProgramPanel, int>>((zero, sepp_self) =>
		{
			return ForLoopIteratorStart_Parts(sepp_self);
		});
	}
}
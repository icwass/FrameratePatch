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

	public static MethodInfo PrivateMethod<T>(string method) => typeof(T).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

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
	}

	////////////////////////////////////////////////////////////////////////////////////////////////////
	// improve framerate by: only compiling the instruction tape when necessary

	private static string storedID = "";
	private static Maybe<CompiledProgramGrid> storedCPG;
	private static bool updateStoredCPG = true;

	public static void RequestTapeRecompile(On.Solution.orig_method_1965 orig, Solution solution_self)
	{
		orig(solution_self);
		updateStoredCPG = true;
	}

	public static string get_CPG_ID(Solution solution)
	{
		string solutionID = solution.field_3915;
		Puzzle puzzle = solution.method_1934();
		string puzzleID = puzzle.field_2766;
		return solutionID + puzzleID;
	}

	public static Maybe<CompiledProgramGrid> CompileTapeOnlyWhenNeeded(On.CompiledProgramGrid.orig_method_855 orig, Solution solution)
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

}
using System;
using System.IO;
using System.Collections.Generic;
using Pipliz;
using Pipliz.Chatting;
using Pipliz.JSON;
using Pipliz.Threading;
using Pipliz.APIProvider.Recipes;
using Pipliz.APIProvider.Jobs;
using NPC;
using System.Threading;
using BlockTypes.Builtin;

namespace ScarabolMods
{
  [ModLoader.ModManager]
  public static class SettlersModEntries
  {
    public static string ModDirectory;

    [ModLoader.ModCallback (ModLoader.EModCallbackType.OnAssemblyLoaded, "scarabol.settlers.assemblyload")]
    public static void OnAssemblyLoaded (string path)
    {
      ModDirectory = Path.GetDirectoryName (path);
    }

    [ModLoader.ModCallback (ModLoader.EModCallbackType.AfterStartup, "scarabol.settlers.registercallbacks")]
    public static void AfterStartup ()
    {
      Pipliz.Log.Write ("Loaded Settlers Mod 1.0 by Scarabol");
    }

    [ModLoader.ModCallback (ModLoader.EModCallbackType.AfterNetworkSetup, "scarabol.settlers.afternetworksetup")]
    public static void AfterNetworkSetup ()
    {
      SettlersTracker.LoadAndConnect ();
    }

    [ModLoader.ModCallback (ModLoader.EModCallbackType.OnTryChangeBlockUser, "scarabol.settlers.ontrychangeblockuser")]
    public static bool OnTryChangeBlockUser (ModLoader.OnTryChangeBlockUserData data)
    {
      // TODO remove block logging
      Pipliz.Log.Write (string.Format ("{0}, {1}, {2}, {3}, {4}, {5}", data.isPrimaryAction, data.requestedBy, data.voxelHit, data.voxelHitSide, data.typeSelected, data.typeToBuild));
      return true;
    }
  }
}
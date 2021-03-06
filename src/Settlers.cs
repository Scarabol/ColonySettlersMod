﻿using System;
using System.IO;
using System.Collections.Generic;
using Pipliz;
using Pipliz.Chatting;
using Pipliz.JSON;
using Pipliz.Threading;
using NPC;
using System.Threading;
using BlockTypes.Builtin;

namespace ScarabolMods
{
  [ModLoader.ModManager]
  public static class SettlersModEntries
  {
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
  }
}
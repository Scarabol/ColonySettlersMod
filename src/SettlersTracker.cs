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
  public static class SettlersTracker
  {
    private static List<SettlersManager> settlers = new List<SettlersManager> ();
    private static ulong nextId = 742000000;

    // FIXME save and load from savegame folder
    public static void LoadAndConnect ()
    {
      try {
        JSONNode json;
        if (Pipliz.JSON.JSON.Deserialize (Path.Combine (SettlersModEntries.ModDirectory, "settlers.json"), out json, false)) {
          List<SettlersManager> loadedSettlers = new List<SettlersManager> ();
          JSONNode jsonSettlers;
          if (!json.TryGetAs ("settlers", out jsonSettlers) || jsonSettlers.NodeType != NodeType.Array) {
            Pipliz.Log.WriteError ("No 'settlers' array found in settlers.json");
            return;
          }
          foreach (JSONNode jsonSettlerNode in jsonSettlers.LoopArray()) {
            SettlersManager manager = new SettlersManager (jsonSettlerNode);
            nextId = System.Math.Max (nextId, manager.SteamID);
            loadedSettlers.Add (manager);
          }
          settlers = loadedSettlers;
          foreach (SettlersManager manager in settlers) {
            manager.Connect ();
          }
        }
      } catch (Exception exception) {
        Pipliz.Log.WriteError (string.Format ("Exception while loading settlers; {0}", exception.Message));
      }
    }

    public static void Save ()
    {
      try {
        JSONNode jsonSettlers = new JSONNode (NodeType.Array);
        foreach (SettlersManager manager in settlers) {
          jsonSettlers.AddToArray (manager.toJSON ());
        }
        JSONNode jsonFileNode = new JSONNode ();
        jsonFileNode.SetAs ("settlers", jsonSettlers);
        Pipliz.JSON.JSON.Serialize (Path.Combine (SettlersModEntries.ModDirectory, "settlers.json"), jsonFileNode, 3);
      } catch (Exception exception) {
        Pipliz.Log.WriteError (string.Format ("Exception while saving settlers; {0}", exception.Message));
      }
    }

    public static ulong GetNextID ()
    {
      nextId++;
      return nextId;
    }

    public static void StartSettlement (int startx, int startz)
    {
      Vector3Int origin = new Vector3Int (startx, 0, startz);
      origin.y = TerrainGenerator.GetHeight (origin.x, origin.z);
      SettlersManager manager = new SettlersManager (GetNextID (), origin);
      settlers.Add (manager);
      manager.Connect ();
      Save ();
    }
  }
}
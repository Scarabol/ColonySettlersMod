using System;
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
  public static class SettlersTracker
  {
    private static List<SettlersManager> settlers = new List<SettlersManager> ();
    private static ulong nextId = 742000000;

    public static void LoadAndConnect ()
    {
      try {
        string jsonFilePath = Path.Combine (Path.Combine ("gamedata", "savegames"), Path.Combine (ServerManager.WorldName, "settlers.json"));
        JSONNode json;
        if (Pipliz.JSON.JSON.Deserialize (jsonFilePath, out json, false)) {
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
        string jsonFilePath = Path.Combine (Path.Combine ("gamedata", "savegames"), Path.Combine (ServerManager.WorldName, "settlers.json"));
        Pipliz.JSON.JSON.Serialize (jsonFilePath, jsonFileNode, 3);
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
      origin.y = TerrainGenerator.GetHeight (origin.x, origin.z) + 1;
      SettlersManager manager = new SettlersManager (GetNextID (), origin);
      settlers.Add (manager);
      manager.Connect ();
      Save ();
    }
  }
}
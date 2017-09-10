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
  public class SettlersChatCommand : ChatCommands.IChatCommand
  {
    [ModLoader.ModCallback (ModLoader.EModCallbackType.AfterItemTypesServer, "scarabol.settlers.registercommand")]
    public static void AfterItemTypesServer ()
    {
      ChatCommands.CommandManager.RegisterCommand (new SettlersChatCommand ());
    }

    public bool IsCommand (string chat)
    {
      return chat.Equals ("/settlers");
    }

    public bool TryDoCommand (Players.Player causedBy, string chattext)
    {
      try {
        Vector3Int origin = causedBy.VoxelPosition;
        SettlersTracker.StartSettlement (origin.x, origin.z);
        Chat.Send (causedBy, $"started new settlement at {origin.x} {origin.z}");
      } catch (Exception exception) {
        Pipliz.Log.WriteError (string.Format ("Exception while parsing command; {0}", exception.Message));
      }
      return true;
    }
  }
}
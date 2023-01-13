using CSF;
using CSF.TShock;

namespace TileWatch.Commands
{
    [RequirePermission("tilewatch.history")]
    internal class HistoryCommand : TSModuleBase<TSCommandContext>
    {
        [Command("history", "h", "twh", "checktile", "ct")]
        public IResult History()
        {
            var player = Context.Player;
            if (player == null)
                return Error("You must be a player to use this command.");

            if (player.GetData<bool>("usinghistory"))
                return Error("You are already using history.");

            player.SetData("usinghistory", true);
            return Success("Hit a block to get its history!");
        }


    }
}

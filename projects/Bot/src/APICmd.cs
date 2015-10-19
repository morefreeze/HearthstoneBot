using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HearthstoneBot
{
    class APICmd
    {
        public static Player getOurPlayer()
        {
            return GameState.Get().GetFriendlySidePlayer();
        }

        public static Player getEnemyPlayer()
        {
            return GameState.Get().GetFirstOpponentPlayer(getOurPlayer());
        }
        public int getCrystals(string whos)
        {
            switch (whos)
            {
                case "ENEMY_HERO":
                    return getEnemyPlayer().GetNumAvailableResources();

                case "OUR_HERO":
                    return getOurPlayer().GetNumAvailableResources();

                default:
                    Log.error("getCrystals: Unknown who requested = " + whos);
                    // Return nothing
                    return 0;
            }
        }
        public void addCrystals(int num)
        {
            getOurPlayer().AddManaCrystal(num);
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Contracts {

    public class Zeal : Contract {

        protected new int[] pointsPerLevel = Constants.ZEAL_POINTS_PER_LEVEL;

        public Zeal() {
            progressToNextLevel = Constants.ZEAL_PROGRESS_PER_LEVEL[0];
            contractID = 2;
        }
        
        protected override int AwardPoints() {
            Debug.Log("Awarding Zeal points!");
            // Updating progress needed for completing next level
            progressToNextLevel = level >= Constants.ZEAL_PROGRESS_PER_LEVEL.Length
                ? Constants.ZEAL_PROGRESS_PER_LEVEL[^1] // Regex for last element
                : Constants.ZEAL_PROGRESS_PER_LEVEL[level-1];
            
            // Returning points awarded for current contract level
            return level >= Constants.ZEAL_POINTS_PER_LEVEL.Length
                ? Constants.ZEAL_POINTS_PER_LEVEL[^1] // Regex for last element
                : Constants.ZEAL_POINTS_PER_LEVEL[level-1];
        }
    }
}

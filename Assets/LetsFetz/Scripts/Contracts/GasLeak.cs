using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Contracts {

    public class GasLeak : Contract {

        protected new int[] pointsPerLevel = Constants.GASLEAK_POINTS_PER_LEVEL;
        
        public GasLeak() {
            progressToNextLevel = Constants.GASLEAK_PROGRESS_PER_LEVEL[0];
            contractID = 1;
        }
        
        protected override int AwardPoints() {
            Debug.Log("Awarding Gas Leak points!");
            // Updating progress needed for completing next level
            progressToNextLevel = level >= Constants.GASLEAK_PROGRESS_PER_LEVEL.Length
                ? Constants.GASLEAK_PROGRESS_PER_LEVEL[^1] // Regex for last element
                : Constants.GASLEAK_PROGRESS_PER_LEVEL[level-1];
            
            // Returning points awarded for current contract level
            return level >= Constants.GASLEAK_POINTS_PER_LEVEL.Length
                ? Constants.GASLEAK_POINTS_PER_LEVEL[^1] // Regex for last element
                : Constants.GASLEAK_POINTS_PER_LEVEL[level-1];
        }
    }
}
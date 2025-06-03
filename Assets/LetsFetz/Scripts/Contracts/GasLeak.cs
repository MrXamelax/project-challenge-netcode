using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Contracts {

    public class GasLeak : Contract {

        //protected int[] pointsPerLevel = Constants.GASLEAK_POINTS_PER_LEVEL;
        
        public GasLeak() {
            progressToNextLevel = Constants.GASLEAK_PROGRESS_PER_LEVEL[0];
            contractID = 1;
            pointsPerLevel = Constants.GASLEAK_POINTS_PER_LEVEL;
        }

        public override int GetProgressNeeded() {
            return level >= Constants.GASLEAK_PROGRESS_PER_LEVEL.Length
                ? Constants.GASLEAK_PROGRESS_PER_LEVEL[^1] // Regex for last element
                : Constants.GASLEAK_PROGRESS_PER_LEVEL[level];
        }
        
        protected override int AwardPoints() {
            //Debug.Log("Awarding Gas Leak points!");
            if (level >= Constants.GASLEAK_POINTS_PER_LEVEL.Length) {
                progressToNextLevel = level >= Constants.GASLEAK_PROGRESS_PER_LEVEL.Length-1
                    ? Constants.GASLEAK_PROGRESS_PER_LEVEL[^1] // Regex for last element
                    : Constants.GASLEAK_PROGRESS_PER_LEVEL[level-1];
                level = Constants.GASLEAK_POINTS_PER_LEVEL.Length - 1;
            } else {
                progressToNextLevel = level >= Constants.GASLEAK_PROGRESS_PER_LEVEL.Length-1
                    ? Constants.GASLEAK_PROGRESS_PER_LEVEL[^1] // Regex for last element
                    : Constants.GASLEAK_PROGRESS_PER_LEVEL[level];
            }
            
            return Constants.GASLEAK_POINTS_PER_LEVEL[level];
            
            // Updating progress needed for completing next level
            progressToNextLevel = level >= Constants.GASLEAK_PROGRESS_PER_LEVEL.Length
                ? Constants.GASLEAK_PROGRESS_PER_LEVEL[^1] // Regex for last element
                : Constants.GASLEAK_PROGRESS_PER_LEVEL[level];
            
            // Returning points awarded for current contract level
            return level >= Constants.GASLEAK_POINTS_PER_LEVEL.Length
                ? Constants.GASLEAK_POINTS_PER_LEVEL[^1] // Regex for last element
                : Constants.GASLEAK_POINTS_PER_LEVEL[level-1];
        }
    }
}
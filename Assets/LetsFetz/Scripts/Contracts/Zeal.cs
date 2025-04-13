using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Contracts {

    public class Zeal : Contract {

        //protected new int[] pointsPerLevel = Constants.ZEAL_POINTS_PER_LEVEL;

        public Zeal() {
            progressToNextLevel = Constants.ZEAL_PROGRESS_PER_LEVEL[0];
            contractID = 2;
            pointsPerLevel = Constants.ZEAL_POINTS_PER_LEVEL;
        }

        public override int GetProgressNeeded() {
            return level >= Constants.ZEAL_PROGRESS_PER_LEVEL.Length
                ? Constants.ZEAL_PROGRESS_PER_LEVEL[^1] // Regex for last element
                : Constants.ZEAL_PROGRESS_PER_LEVEL[level];
        }

        protected override int AwardPoints() {
            Debug.Log("Awarding Zeal points!");
            if (level >= Constants.ZEAL_POINTS_PER_LEVEL.Length) {
                progressToNextLevel = level >= Constants.ZEAL_PROGRESS_PER_LEVEL.Length-1
                    ? Constants.ZEAL_PROGRESS_PER_LEVEL[^1] // Regex for last element
                    : Constants.ZEAL_PROGRESS_PER_LEVEL[level-1];
                level = Constants.ZEAL_POINTS_PER_LEVEL.Length - 1;
            } else {
                progressToNextLevel = level >= Constants.ZEAL_PROGRESS_PER_LEVEL.Length-1
                    ? Constants.ZEAL_PROGRESS_PER_LEVEL[^1] // Regex for last element
                    : Constants.ZEAL_PROGRESS_PER_LEVEL[level];
            }
            
            return Constants.ZEAL_POINTS_PER_LEVEL[level];
            
            // Updating progress needed for completing next level
            progressToNextLevel = level >= Constants.ZEAL_PROGRESS_PER_LEVEL.Length
                ? Constants.ZEAL_PROGRESS_PER_LEVEL[^1] // Regex for last element
                : Constants.ZEAL_PROGRESS_PER_LEVEL[level];
            
            // Returning points awarded for current contract level
            return level >= Constants.ZEAL_POINTS_PER_LEVEL.Length
                ? Constants.ZEAL_POINTS_PER_LEVEL[^1] // Regex for last element
                : Constants.ZEAL_POINTS_PER_LEVEL[level-1];
        }
    }
}

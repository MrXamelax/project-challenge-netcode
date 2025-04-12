using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Contracts {

    public class DataStation : Contract {

        //protected new int[] pointsPerLevel = Constants.DATASTATION_POINTS_PER_LEVEL;

        public DataStation() {
            progressToNextLevel = Constants.DATASTATION_PROGRESS_PER_LEVEL[0];
            contractID = 0;
            pointsPerLevel = Constants.DATASTATION_POINTS_PER_LEVEL;
        }

        public override int GetProgressNeeded() {
            return level >= Constants.DATASTATION_PROGRESS_PER_LEVEL.Length
                ? Constants.DATASTATION_PROGRESS_PER_LEVEL[^1] // Regex for last element
                : Constants.DATASTATION_PROGRESS_PER_LEVEL[level];
        }

        protected override int AwardPoints() {
            Debug.Log("Awarding Data Station points!");
            if (level >= Constants.DATASTATION_POINTS_PER_LEVEL.Length) {
                progressToNextLevel = level >= Constants.DATASTATION_PROGRESS_PER_LEVEL.Length-1
                    ? Constants.DATASTATION_PROGRESS_PER_LEVEL[^1] // Regex for last element
                    : Constants.DATASTATION_PROGRESS_PER_LEVEL[level-1];
                level = Constants.DATASTATION_POINTS_PER_LEVEL.Length - 1;
            } else {
                progressToNextLevel = level >= Constants.DATASTATION_PROGRESS_PER_LEVEL.Length-1
                    ? Constants.DATASTATION_PROGRESS_PER_LEVEL[^1] // Regex for last element
                    : Constants.DATASTATION_PROGRESS_PER_LEVEL[level-1];
            }

            return Constants.DATASTATION_POINTS_PER_LEVEL[level];
            
            Debug.Log("Awarding Data Station points!");
            // Updating progress needed for completing next level
            progressToNextLevel = level >= Constants.DATASTATION_PROGRESS_PER_LEVEL.Length-1
                ? Constants.DATASTATION_PROGRESS_PER_LEVEL[^1] // Regex for last element
                : Constants.DATASTATION_PROGRESS_PER_LEVEL[level];
            
            // Returning points awarded for current contract level
            return level >= Constants.DATASTATION_POINTS_PER_LEVEL.Length
                ? Constants.DATASTATION_POINTS_PER_LEVEL[^1] // Regex for last element
                : Constants.DATASTATION_POINTS_PER_LEVEL[level-1];
        }
    }
}
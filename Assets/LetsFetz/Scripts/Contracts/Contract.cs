using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Contract {

    protected int level = 0;
    protected int contractID;
    protected int progressToNextLevel;
    protected int[] pointsPerLevel;

    private void LevelUp() {
        level += 1;
    }
    
    // Returns true if we leveled up
    public int AddProgress(int progress) {
        progressToNextLevel -= progress;

        if (progressToNextLevel <= 0) {
            level += 1;
            return AwardPoints();
        }

        return 0;
    }

    public int GetContractID() {
        return contractID;
    }
    
    public int[] GetPointsPerLevel() {
        return pointsPerLevel;
    }
    
    protected abstract int AwardPoints();

}

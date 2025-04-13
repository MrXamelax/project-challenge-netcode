using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Contract {

    protected int level = 0;
    protected int contractID;
    protected int progressToNextLevel;
    
    protected int[] progressPerLevel;
    
    protected int[] pointsPerLevel;
    
    public int AddProgress(int progress) {
        progressToNextLevel -= progress;

        if (progressToNextLevel <= 0) {
            level += 1;
            return AwardPoints();
        }
        
        return 0;
    }
    
    public int GetProgressToNextLevel() {
        return progressToNextLevel;
    }

    public int GetContractID() {
        return contractID;
    }
    
    public int[] GetPointsPerLevel() {
        return pointsPerLevel;
    }

    public int GetPointsPerLevelCurrent() {
        return pointsPerLevel[level];
    }
    
    public int GetLevel() {
        return level;
    }

    #region Setters
    // Only needed for synchronization, initiated by server
    
    public void SetProgressToNextLevel(int progressToNextLevel) {
        this.progressToNextLevel = progressToNextLevel;
    }

    public void SetLevel(int level) {
        this.level = level;
    }
    
    #endregion
    
    protected abstract int AwardPoints();

    public abstract int GetProgressNeeded();

}

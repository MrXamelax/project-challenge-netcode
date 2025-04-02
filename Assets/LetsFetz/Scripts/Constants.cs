using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

// Class containing constants used by other systems
// Value balancing
public class Constants {


    // Data Station Values

    #region Data Station

    // Time needed to capture a data station
    public const int DATASTATION_CAPTURE_TIME
        = 3;

    // Unique GameObject name to find the object in the scene
    public const string DATASTATION_GAMEOBJECT_NAME = "DatastationFunctional";

    // Time needed to pass for a singular progress tick
    public const int DATASTATION_TIME_PER_TICK
        = 4;

    // Progression value added to contract per tick, mostly for ratio
    public const int DATASTATION_PROGRESS_PER_TICK
        = 10;

    // Total progression value needed to level up for corresponding level
    public static readonly int[] DATASTATION_PROGRESS_PER_LEVEL
        = { 50, 70, 100, 120 };

    // Points awarded to team by completing the full progress of the corresponding level
    public static readonly int[] DATASTATION_POINTS_PER_LEVEL
        = { 20, 20, 30, 50 };

    #endregion


    // Gas Leak Values

    #region Gas Leak

    // Time needed to capture a gas leak
    public const float GASLEAK_CAPTURE_TIME
        = 0.5f;

    // Unique GameObject tag to find matching objects in the scene
    public const string GASLEAK_GAMEOBJECT_TAG = "GasLeak";

    // Time needed to pass for a singular progress tick
    public const int GASLEAK_TIME_PER_TICK
        = 2;

    // Progression value added to contract per tick corresponding to refiner level
    public static readonly int[] GASLEAK_PROGRESS_PER_TICK_BY_LEVEL
        = { 1, 5, 15 };

    public static readonly int[] GASLEAK_TIME_TO_LEVELUP
        = { 10, 30, 60 };

    // Total progression value needed to level up for corresponding level
    public static readonly int[] GASLEAK_PROGRESS_PER_LEVEL
        = { 100, 250, 500, 600, 800, 1000, 1250, 1500 };

    // Points awarded to team by completing the full progress of the corresponding level
    public static readonly int[] GASLEAK_POINTS_PER_LEVEL
        = { 5, 10, 15, 20, 25, 35, 45, 50 };

    #endregion


    // Zeal Values

    #region Zeal
    
    // Unique GameObject name to find the object in the scene
    public const string ZEAL_YELLOW_GAMEOBJECT_NAME = "ZealYellow";
    
    // Unique GameObject name to find the object in the scene
    public const string ZEAL_RED_GAMEOBJECT_NAME = "ZealRed";
    
    // Time in seconds needed in Match to pass before the yellow zeal is spawned by the host
    public const int ZEAL_YELLOW_SPAWN_TIMESTAMP = 2;
    
    // Time in seconds needed in Match to pass before the red zeal is spawned by the host
    public const int ZEAL_RED_SPAWN_TIMESTAMP = 3;
    
    #endregion


public const int MAX_HEALTH 
        = 100;
    
    public static ReadOnlyDictionary<int, Contract> CONTRACT_MAP =
        new ReadOnlyDictionary<int, Contract>(new Dictionary<int, Contract> {
            {0, new Contracts.DataStation()},
            {1, new Contracts.GasLeak()}
        });

}

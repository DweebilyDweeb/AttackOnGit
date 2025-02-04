﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using GridID = System.Int32;

public class MonsterSpawner : MonoBehaviour {

	//These will be removed in the final product.
	public bool temporaryTestStartStageButton;
    public bool temporaryTestStopStageButton;
    [SerializeField]
	private int tempStageIndex;
	[SerializeField]
	private bool hasActiveMonsters;

	[SerializeField]
	private bool hasBoss;
	[SerializeField]
	private int bossCheckStageIndex;

	//These 2 have priority.
	[SerializeField]
	private Grid startGrid;
	[SerializeField]
	private Grid endGrid;

	//These 2 will be used if startGrid or endGrid is null. If these 2 are used, gridSystem must not be null.
	[SerializeField]
	private GridID startID, endID; //There should not be an underscore. That was a mistake.

	//Only needed if startGrid or endGrid is null.
	[SerializeField]
	private GridSystem gridSystem;

	[SerializeField]
	private List<GridID> path;
	[SerializeField]
	private List<GridID> shortestPath;

	[SerializeField]
	private GameObject[] monsterPrefabs = new GameObject[(uint)MONSTER_TYPE.NUM_MONSTER_TYPE]; //Do not change the size!
	private List<GameObject>[] monsterPool;
	private int[] monsterPoolIndex; //Our last searched index;

	[SerializeField]
	private GameObject[] bossPrefabs = new GameObject[(uint)BOSS_TYPE.NUM_BOSS_TYPE];
	private List<GameObject> bossList; //Don't do object pooling for boss. Not worth it.

    // Use this for like, initialization even before initialization. Initializationception!
	void Awake() {
		GeneratePath();
		monsterPool = new List<GameObject>[(uint)MONSTER_TYPE.NUM_MONSTER_TYPE];
		for (int i = 0; i < monsterPool.Length; ++i) {
			monsterPool[i] = new List<GameObject>();
		}
		monsterPoolIndex = new int[(uint)MONSTER_TYPE.NUM_MONSTER_TYPE];
		for (int i = 0; i < monsterPoolIndex.Length; ++i) {
			monsterPoolIndex[i] = 0;
		}

		bossList = new List<GameObject>();

        temporaryTestStartStageButton = false;
        temporaryTestStopStageButton = false;
    }

	// Use this for initialization
	void Start () {
	}

	// Update is called once per frame
	void Update () {
		if (temporaryTestStartStageButton) {
			StartStage(tempStageIndex);
			temporaryTestStartStageButton = false;
			++tempStageIndex;
		}
        if (temporaryTestStopStageButton) {
            StopAllStages();
            temporaryTestStopStageButton = false;
            tempStageIndex = 0;
        }
		hasActiveMonsters = HasActiveMonsters();
		hasBoss = HasBoss(bossCheckStageIndex);

		//Don't change the size. If you do I'll just keep changing it back.
		//We migh also crash. Probably crash.
		if (monsterPrefabs.Length != (uint)MONSTER_TYPE.NUM_MONSTER_TYPE) {
			monsterPrefabs = new GameObject[(uint)MONSTER_TYPE.NUM_MONSTER_TYPE];
		}

		if (bossPrefabs.Length != (uint)BOSS_TYPE.NUM_BOSS_TYPE) {
			bossPrefabs = new GameObject[(uint)BOSS_TYPE.NUM_BOSS_TYPE];
		}
	}

	public bool GeneratePath() {
		if (startGrid == null || endGrid == null) {
			if (gridSystem.GetGrid(startID) != null) {
				startGrid = gridSystem.GetGrid(startID).GetComponent<Grid>();
			}
			if (gridSystem.GetGrid(endID) != null) {
				endGrid = gridSystem.GetGrid(endID).GetComponent<Grid>();
			}
		} else {
			//Alrighty, we have start and end grids. Let's find our path!
			gridSystem = null; //Set this to null first.
			//Get the Grid System.
			GridSystem startGridSystem = startGrid.GetGridSystem();
			GridSystem endGridSystem = endGrid.GetGridSystem();
			if (startGridSystem == null || endGridSystem == null) {
				print(gameObject.name + "'s start grid or end grid has no grid system.");
			} else if (startGridSystem != endGridSystem) {
				print(gameObject.name + "'s start grid and end grid have different grid system. Seriously? What is wrong with you?");
			} else {
				gridSystem = startGridSystem; //Gotcha!
				startID = startGrid.GetID(); //Assign our startID.
				endID = endGrid.GetID(); //Assign our endID.
			}
		}

		if (gridSystem == null) {
			print(gameObject.name + " has no Grid System."); //失败！/(T.T)\
            return false;
		} else {
			path = gridSystem.Search(startID, endID); //成功！\(^.^)/
			shortestPath = gridSystem.Search(startID, endID, true);
            if (path == null || shortestPath == null) {
                return false;
            }
            return true;
		}
	}

	public bool SpawnMonster(MONSTER_TYPE _monsterType) {
		if (_monsterType == MONSTER_TYPE.NUM_MONSTER_TYPE) {
			print(gameObject.name + " cannot spawn monster as monster type is invalid.");
			return false;
		} else if (monsterPrefabs[(uint)_monsterType] == null) {
			print(gameObject.name + " cannot spawn monster as monster type prefab is null.");
			return false;
		} else if (monsterPrefabs[(uint)_monsterType].GetComponent<AIFollowPath>() == null) {
			print(gameObject.name + " cannot spawn a monster without AIFollowPath Component!");
			return false;
		}

		if (path != null && path.Count > 0) { //Place the monster at the start of the path.			
			GameObject monster = GetMonster(_monsterType);
			monster.SetActive(true);
			monster.GetComponent<AIFollowPath>().gridSystem = gridSystem;
			monster.GetComponent<AIFollowPath>().path = path;
			monster.GetComponent<AIAttackCrystal>().tritanCrystal = endGrid.tritanCrystal;
			monster.GetComponent<Transform>().position = gridSystem.GetGrid(path[0]).GetComponent<Transform>().position + new Vector3(0, 0.2f, 0);
			monster.GetComponent<AIBehaviour>().Reset();
			return true;
		} else {
			print("Cannot spawn monster without a path!");
			return false;
		}
	}

	public bool SpawnBoss(BOSS_TYPE _bossType) {
		if (_bossType == BOSS_TYPE.NUM_BOSS_TYPE) {
			print(gameObject.name + " cannot spawn boss as boss type is invalid.");
			return false;
		} else if (bossPrefabs[(uint)_bossType] == null) {
			print(gameObject.name + " cannot spawn boss as boss type prefab is null.");
			return false;
		} else if (bossPrefabs[(uint)_bossType].GetComponent<AIFollowPath>() == null) {
			print(gameObject.name + " cannot spawn a boss without AIFollowPath Component!");
			return false;
		}

		if (path != null && path.Count > 0) { //Place the monster at the start of the path.			
			GameObject boss = GameObject.Instantiate(bossPrefabs[(uint)_bossType]);
			boss.GetComponent<AIFollowPath>().gridSystem = gridSystem;
			boss.GetComponent<AIFollowPath>().path = shortestPath;
			boss.GetComponent<AIAttackCrystal>().tritanCrystal = endGrid.tritanCrystal;
			boss.GetComponent<Transform>().position = gridSystem.GetGrid(path[0]).GetComponent<Transform>().position + new Vector3(0, 0.2f, 0);
			bossList.Add(boss);
			return true;
		} else {
			print("Cannot spawn boss without a path!");
			return false;
		}
	}

	private GameObject GetMonster(MONSTER_TYPE _monsterType) {
		GameObject monster = null; //The monster.
		if (monsterPool[(uint)_monsterType].Count > 0) {			
			//Which index to start searching from.
			int currentIndex = (monsterPoolIndex[(uint)_monsterType] + 1) % monsterPool[(uint)_monsterType].Count;

			//Stop looping once we've checked everything.
			for (int i = 0; i < monsterPool[(uint)_monsterType].Count; ++i) {
				int index = (currentIndex + i) % monsterPool[(uint)_monsterType].Count;
				monster = monsterPool[(uint)_monsterType][index];
				if (monster == null || monster.activeSelf == true) {
					continue; //This one is not suitable.
				}
				monsterPoolIndex[(uint)_monsterType] = index;
				return monster;
			}
		}

		monster = GameObject.Instantiate(monsterPrefabs[(uint)_monsterType]);
		monsterPool[(uint)_monsterType].Add(monster);
		monsterPoolIndex[(uint)_monsterType] = 0;

		return monster;
	}

	public void ClearMonsters() {
		for (int monsterType = 0; monsterType < (int)MONSTER_TYPE.NUM_MONSTER_TYPE; ++monsterType) {
			foreach (GameObject monster in monsterPool[monsterType]) {
				if (monster != null) {
					GameObject.Destroy(monster);
				}
			}
			monsterPool[monsterType].Clear();
		}

		for (int i = 0; i < bossList.Count; ++i) {
			if (bossList[i] == null) {
				continue;
			}
			GameObject.Destroy(bossList[i]);
		}
		bossList.Clear();
	}

	public int GetStartID() {
		return startID;
	}

	public int GetEndID() {
		return endID;
	}

	public Grid GetStartGrid() {
		return startGrid;
	}

	public Grid GetEndGrid() {
		return endGrid;
	}

	//These 2 functions are just a temporary fix for one of my typos. Gotta fix it eventually.
	public int _startID {
		get {
			return this.startID;
		}
	}

	public int _endID {
		get {
			return this.endID;
		}
	}

	//Start the action.
	public bool StartStage(int _stageIndex) {
		if (_stageIndex < 0) {
			return false;
		}
        
		int index = 0;
		foreach (Transform child in transform) {
			//Check if it is a stage.
			MonsterSpawnerStage stage = child.gameObject.GetComponent<MonsterSpawnerStage>();
			if (stage == null) {
				continue;
			}

			//Yes, this is the right one. We can stop now.
			if (_stageIndex == index++) {
                //Stop all other stages.
                StopAllStages();
				stage.StartStage();
				return true;
			}
		}

		return false;
	}

	//If for whatever reason we need to stop it, like when the game ends.
	public void StopAllStages() {
		foreach (Transform child in transform) {
			//Check if it is a stage.
			MonsterSpawnerStage stage = child.gameObject.GetComponent<MonsterSpawnerStage>();
			if (stage == null) {
				continue;
			}

			stage.StopStage(); //停！
		}
        ClearMonsters();
	}

	//This seems... unoptimised. No noticable framerate decrease for now.
    public bool HasActiveMonsters() {
		for (int i = 0; i < monsterPool.Length; ++i) {
			for (int j = 0; j < monsterPool[i].Count; ++j) {
				if (monsterPool[i][j] == null) {
					continue;
				}
				if (monsterPool[i][j].activeSelf == true) {
					return true;
				}
			}
		}

		for (int i = 0; i < bossList.Count; ++i) {
			if (bossList[i] == null) {
				continue;
			}
			if (bossList[i].activeSelf == true) {
				return true;
			}
		}
		return false;
    }

	public bool HasBoss(int _stageIndex) {
		if (_stageIndex < 0) {
			return false;
		}

		int index = 0;
		foreach (Transform child in gameObject.transform) {
			//Check if it is a stage.
			MonsterSpawnerStage stage = child.gameObject.GetComponent<MonsterSpawnerStage>();
			if (stage == null) {
				continue;
			}

			//Yes, this is the right one. We can stop now.
			if (_stageIndex == index++) {
				return stage.HasBoss();
			}
		}

		return false;
	}

}
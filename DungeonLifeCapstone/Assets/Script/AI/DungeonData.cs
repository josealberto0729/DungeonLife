using UnityEngine;
[System.Serializable]
public class DungeonData
{
    public Room[] rooms;
    public Powerup[] powerups;
    public string[] objectives;
}

[System.Serializable]
public class Room
{
    public int x;
    public int y;
    public int width;
    public int height;
    public string type;
    public Enemy[] enemies;
    public Powerup[] powerups;
    public Treasure[] treasures;
}

[System.Serializable]
public class Enemy
{
    public string type;
    public int patrolRadius;
    public string ai;
}

[System.Serializable]
public class Treasure
{
    public string type;
    public int x; 
    public int y;
    public int value;
}

[System.Serializable]
public class Powerup
{
    public int x;
    public int y;
    public string type;
}

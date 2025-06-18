using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class DungeonData
{
    public List<Room> rooms;
    public List<Connection> connections;
    public List<Powerup> powerups;
    public List<string> objectives;
}
[System.Serializable]
public class Connection
{
    public int fromX;
    public int fromY;
    public int toX;
    public int toY;
}

[System.Serializable]
public class Room
{
    public int x;
    public int y;
    public int width;
    public int height;
    public string type;
    public List<Enemy> enemies;
    public List<Powerup> powerups;
    public List<Treasure> treasures;
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

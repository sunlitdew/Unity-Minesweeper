using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Random = System.Random;

static class Extension
{
    private static Random _rng = new Random();
    public static void Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1) {
            n--;
            int k = _rng.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }
}


public class GameManager : MonoBehaviour
{

    public GameObject tilePrefab;

    public GameObject board;
    public GameObject tileSpace;
    public Image tism;

    public Sprite happyTism;
    public Sprite sadTism;

    public static GameManager Instance;

    private List<List<Tile>> _gameMap;
    private bool _isGenerated = false;
    private bool _isActive = true;
    public int width = 10;
    public int height = 10;

    private int NumberOfMines => height * width / 6;
    private List<Tile> _mineList = new List<Tile>();


    public int MinesLeft { get; private set; }

    private int _emptyLeft;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        Reset();
    }

    public void Reset()
    {
        _isGenerated = false;
        _isActive = true;
        tism.sprite = happyTism;
        MinesLeft = NumberOfMines;
        _mineList = new List<Tile>();
        _emptyLeft = width * height - NumberOfMines;
        while (tileSpace.transform.childCount > 0)
        {
            var child = tileSpace.transform.GetChild(0);
            child.parent = null;
            Destroy(child.gameObject);
        }

        board.GetComponent<AspectRatioFitter>().aspectRatio = (float) width / height;

        var component = board.GetComponent<RectTransform>();
        LayoutRebuilder.ForceRebuildLayoutImmediate(component);

        var tileSize = component.rect.width / width;

        tileSpace.GetComponent<GridLayoutGroup>().cellSize = new Vector2(tileSize, tileSize);



        _gameMap = new List<List<Tile>>();
        for (int i = 0; i < width; i++)
        {
            _gameMap.Add(new List<Tile>());
            for (int j = 0; j < height; j++)
            {
                var tile = Instantiate(tilePrefab, tileSpace.transform);
                var script = tile.GetComponent<Tile>();
                script.Init(i, j);
                _gameMap[i].Add(script);
            }
        }
    }

    public void Reveal(int startX, int startY)
    {
        if (!_isActive) return;

        if (!_isGenerated)
        {
            _isGenerated = true;
            GenerateGame(startX, startY);
        }

        var firstIter = true;
        var revealQueue = new Queue<Tile>();
        revealQueue.Enqueue(_gameMap[startX][startY]);

        while (revealQueue.Count > 0)
        {
            var targetTile = revealQueue.Dequeue();
            if (targetTile.isRevealed)
            {
                if (firstIter)
                {
                    if (targetTile.IsEmpty) continue;
                    var neighbors = new List<Tile>();
                    var foundflags = 0;
                    for (var i = targetTile.x - 1; i <= targetTile.x + 1; i++)
                    {
                        for (var j = targetTile.y - 1; j <= targetTile.y + 1; j++)
                        {
                            if (i < 0 || i >= width || j < 0 || j >= height || (i == targetTile.x && j == targetTile.y)) continue;
                            var neighbor = _gameMap[i][j];
                            if (neighbor.isFlagged)
                            {
                                foundflags++;
                            }
                            else if (!neighbor.isRevealed)
                            {
                                neighbors.Add(neighbor);
                            }
                        }
                    }

                    if (foundflags == targetTile.logicValue)
                    {
                        foreach (var tile in neighbors)
                        {
                            revealQueue.Enqueue(tile);
                        }
                    }
                }
                else
                {
                    continue;
                }
            }
            _emptyLeft--;
            if (targetTile.IsMine)
            {
                GameOver(targetTile);
                return;
            }
            if (targetTile.isFlagged) ToggleFlag(targetTile.x, targetTile.y);
            targetTile.Reveal();
            if (!targetTile.IsEmpty) continue;
            for (var i = targetTile.x - 1; i <= targetTile.x + 1; i++)
            {
                for (var j = targetTile.y - 1; j <= targetTile.y + 1; j++)
                {
                    if (i < 0 || i >= width || j < 0 || j >= height) continue;
                    if (i != targetTile.x || j != targetTile.y)
                    {
                        revealQueue.Enqueue(_gameMap[i][j]);
                    }
                }
            }

            firstIter = false;
        }

        if (_emptyLeft == 0)
        {
            Victory();
        }
    }

    public void ToggleFlag(int x, int y)
    {
        var targetTile =  _gameMap[x][y];
        if (!_isActive || targetTile.isRevealed) return;
        if (targetTile.isFlagged)
        {
            MinesLeft++;
        }
        else
        {
            MinesLeft--;
        }
        targetTile.ToggleFlag();
    }

    private void GenerateGame(int x, int y)
    {
        var emptyTiles = new List<Tile>();
        var protectedTiles = new List<Tile>();
        foreach (var tile in _gameMap.SelectMany(column => column))
        {
            if (Math.Abs(tile.x - x) <= 2 && Math.Abs(tile.y - y) <= 2)
            {
                protectedTiles.Add(tile);
            }
            else
            {
                emptyTiles.Add(tile);
            }
        }

        emptyTiles.Shuffle();
        protectedTiles.Shuffle();
        emptyTiles.AddRange(protectedTiles);
        _mineList.AddRange(emptyTiles.Take(NumberOfMines));
        foreach (var tile in _mineList)
        {
            tile.logicValue = 9;
            for (var i = tile.x - 1; i <= tile.x + 1; i++)
            {
                for (var j = tile.y - 1; j <= tile.y + 1; j++)
                {
                    if (i < 0 || i >= width || j < 0 || j >= height) continue;
                    var neighbor = _gameMap[i][j];
                    if (!neighbor.IsMine)
                    {
                        neighbor.logicValue++;
                    }
                }
            }
        }

        foreach (var tile in _gameMap.SelectMany(column => column))
        {
            tile.UpdateContent();
        }
    }

    private void GameOver(Tile causeTile)
    {
        _isActive = false;
        tism.sprite = sadTism;
        foreach (var tile in _mineList)
        {
            tile.Reveal();
        }
        causeTile.background.color = Color.red;
        causeTile.background.sprite = null;
    }

    private void Victory()
    {
        
    }
}
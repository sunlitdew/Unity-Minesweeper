using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
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
    public TextMeshProUGUI minesLeftTextbox;
    
    public Image tism;

    public Sprite happyTism;
    public Sprite sadTism;

    public static GameManager Instance;
    public EventSystem eventSystem;

    private List<List<Tile>> _gameMap;
    private bool _isGenerated = false;
    private bool _isActive = true;

    public int settingsHeight = 10;
    public int settingsWidth = 10;
    public int width = 10;
    public int height = 10;

    private int NumberOfMines => height * width / 6;
    private List<Tile> _mineList = new List<Tile>();

    public Canvas settingsCanvas;
    public Slider heightSlider;
        
    private bool _settingsOpen = false;
    public bool SettingsOpen
    {
        get => _settingsOpen;
        set
        {
            _settingsOpen = value;
            settingsCanvas.enabled = value;
            EventSystem.current.SetSelectedGameObject(value ? heightSlider.gameObject : null);
        } 
    }


    public int MinesLeft
    {
        get => _minesLeft;
        private set
        {
            _minesLeft = value;
            minesLeftTextbox.text = "Mines Left: " + value;
        }
    }

    private int _minesLeft;

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
        EventSystem.current.SetSelectedGameObject(null);
        _isGenerated = false;
        _isActive = true;
        height = settingsHeight;
        width = settingsWidth;
        tism.sprite = happyTism;
        MinesLeft = NumberOfMines;
        _mineList = new List<Tile>();
        _emptyLeft = width * height - NumberOfMines;
        while (tileSpace.transform.childCount > 0)
        {
            var child = tileSpace.transform.GetChild(0);
            child.SetParent(null);
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

        SetExplicitNavigation();
    }
    
    private void SetExplicitNavigation()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Button btn = _gameMap[x][y].button;
                Navigation nav = new Navigation { mode = Navigation.Mode.Explicit };

                // Link neighbors with boundary checks
                if (y > 0) nav.selectOnUp = _gameMap[x][y - 1].button;
                if (y < height - 1) nav.selectOnDown = _gameMap[x][y + 1].button;
                if (x > 0) nav.selectOnLeft = _gameMap[x - 1][y].button;
                if (x < width - 1) nav.selectOnRight = _gameMap[x + 1][y].button;

                // Apply the locked navigation to the button
                btn.navigation = nav;
            }
        }
    }

    public void Reveal(Tile startTile)
    {
        if (!_isActive) return;

        if (!_isGenerated)
        {
            _isGenerated = true;
            GenerateGame(startTile.x,startTile.y);
        }

        var firstIter = true;
        var revealQueue = new Queue<Tile>();
        revealQueue.Enqueue(startTile);

        while (revealQueue.Count > 0)
        {
            var targetTile = revealQueue.Dequeue();
            if (targetTile.isRevealed)
            {
                if (firstIter)
                {
                    if (targetTile.IsEmpty) continue;
                    var neighbors = new List<Tile>();
                    var foundFlags = 0;
                    for (var i = targetTile.x - 1; i <= targetTile.x + 1; i++)
                    {
                        for (var j = targetTile.y - 1; j <= targetTile.y + 1; j++)
                        {
                            if (i < 0 || i >= width || j < 0 || j >= height || (i == targetTile.x && j == targetTile.y)) continue;
                            var neighbor = _gameMap[i][j];
                            if (neighbor.isFlagged)
                            {
                                foundFlags++;
                            }
                            else if (!neighbor.isRevealed)
                            {
                                neighbors.Add(neighbor);
                            }
                        }
                    }

                    if (foundFlags == targetTile.logicValue)
                    {
                        foreach (var tile in neighbors)
                        {
                            revealQueue.Enqueue(tile);
                        }
                    }

                    continue;
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
            if (targetTile.isFlagged) ToggleFlag(targetTile);
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

    public void ToggleFlag(Tile targetTile)
    {
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
            tile.background.color = Color.red;
            Color tempColor = tile.cover.color;
            tempColor.a = 0.5f;
            tile.cover.color = tempColor;
            if (tile.isFlagged)
            {
                tile.content.enabled = false;
            }
        }
    }

    private void Victory()
    {
        MinesLeft = 0;
        _isActive = false;
        foreach (var tile in _mineList)
        {
            tile.flag.enabled = false;
            tile.background.color = Color.green;
            Color tempColor = tile.cover.color;
            tempColor.a = 0.2f;
            tile.cover.color = tempColor;
        }
    }
    
    public void OnNavigateInput(InputAction.CallbackContext context)
    {
        if (!context.started) return;
        if (EventSystem.current.currentSelectedGameObject == null)
        {
            if (_gameMap != null && _gameMap.Count > 0)
            {
                GameObject startTile = _gameMap[0][0].gameObject;
                EventSystem.current.SetSelectedGameObject(startTile);
            }
        }
    }
    
    
    public void OnRevealInput(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        var selected = EventSystem.current.currentSelectedGameObject;

        if (selected != null && selected.TryGetComponent(out Tile targetTile))
        {
            Debug.Log("Reveal tile x=" + targetTile.x + " y=" + targetTile.y);
            Reveal(targetTile);
        }
    }

    public void OnFlagInput(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        var selected = EventSystem.current.currentSelectedGameObject;

        if (selected != null && selected.TryGetComponent(out Tile targetTile))
        {
            Debug.Log("Flag tile x=" + targetTile.x + " y=" + targetTile.y);
            ToggleFlag(targetTile);
        }
    }

    public void OnToggleSettingsInput(InputAction.CallbackContext context)
    {
        SettingsOpen = !SettingsOpen;
    }
}
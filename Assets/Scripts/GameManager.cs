using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public const int BoardHeight   = 21;
    public const int BoardWidth    = 10;
    
    [Header("UI")]
    public Text         textScore;
    public GameObject   gameOverPanel;
    public GameObject   gamePausedPanel;
    public Image        soundButtonImage;
    public Sprite       spriteSoundOn;
    public Sprite       spriteSoundOff;

    private int         _score;
    
    [Header("Graphics")]
    public Shape[]      shapes;
    public Sprite[]     shapeSprites;
    public Color[]      shapeColors;
    
    public Image        nextShapeSprite;
    
    [Header("Game logic")]
    public int[,]       board = new int[BoardHeight, BoardWidth];
    public int          currentShape;
    public GameObject   singleBlock;
    [Range(0.1f, 1f)]
    public float        fallTime = 1f;
    public HashSet<int> affectedRows = new HashSet<int>();
    
    private int         _nextShape;
    private Vector3[]   _initialShapePositions = new Vector3[7];
    private List<int>   _fullRows = new List<int>();
    private SpriteRenderer[][] _blocksOnBoard = new SpriteRenderer[BoardHeight][];
    
    private AudioSource _audioSource;
    private AudioSource[] _allAudioSources;


    #region GameManager Singleton
    private static GameManager _instance;
    public static GameManager gameManager => _instance;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        } else {
            _instance = this;
        }
    }
    #endregion
    
    // Start is called before the first frame update
    private void Start()
    {
        _audioSource = this.GetComponent<AudioSource>();
        _allAudioSources = FindObjectsOfType<AudioSource>(true);

        // Store initial (spawn) positions of all shapes
        // so we can reposition them after they finish falling
        for (var i = 0; i < shapes.Length; i++)
        {
            _initialShapePositions[i] = shapes[i].transform.position;
        }
        
        // Create Sprites for the board
        for (var i = 0; i < BoardHeight; i++)
        {
            _blocksOnBoard[i] = new SpriteRenderer[BoardWidth];
            for (var j = 0; j < BoardWidth; j++)
            {
                _blocksOnBoard[i][j] = Instantiate(singleBlock, new Vector3(j, i, 0), Quaternion.identity).GetComponent<SpriteRenderer>();
            }
        }

        // Set up new game
        NewGame();
    }

    /*private void Update()
    {
        if (Input.GetKey(KeyCode.DownArrow))
        {
            fallTime = 0.1f;
        }

        if (Input.GetKeyUp(KeyCode.DownArrow))
        {
            fallTime = 1f;
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            TogglePause();
        }
    }*/

    private void NewGame()
    {
        textScore.text = "0";
        _score = 0;
        fallTime = 1f;
        
        affectedRows.Clear();
        _fullRows.Clear();

        // Reset the board state
        for (var row = 0; row < BoardHeight; row++)
        {
            for (var column = 0; column < BoardWidth; column++)
            {
                board[row, column] = 0;
                _blocksOnBoard[row][column].color = shapeColors[0];
            }
        }
        
        for (var i = 0; i < shapes.Length; i++)
        {
            shapes[i].transform.position = _initialShapePositions[i];
            shapes[i].transform.rotation = Quaternion.identity;
        }

        // Spawn the first shape
        currentShape = Random.Range(0, 7);
        _nextShape = Random.Range(0, 7);

        Shape.IsFalling = true;
        gameOverPanel.SetActive(false);
        shapes[currentShape].gameObject.SetActive(true);
        nextShapeSprite.sprite = shapeSprites[_nextShape];
    }

    private void SpawnShape()
    {
        // Rather than to Instantiate a new shape every time, we disable it after
        // it finished falling, and reposition it back to its spawn position. Spawning
        // only re-enables it once again, from the starting position.
        shapes[currentShape].transform.position = _initialShapePositions[currentShape];
        shapes[currentShape].transform.rotation = Quaternion.identity;
        currentShape = _nextShape;
        
        // If the spawn area of the piece is occupied, Game Over
        foreach (Transform block in shapes[currentShape].transform)
        {
            var position = block.position;
            var i = Mathf.RoundToInt(position.y);
            var j = Mathf.RoundToInt(position.x);

            if (board[i, j] != 0)
            {
                GameOver();
                return;
            }
        }
        shapes[currentShape].gameObject.SetActive(true);

        _nextShape = Random.Range(0, 7);
        nextShapeSprite.sprite = shapeSprites[_nextShape];
    }

    private void GameOver()
    {
        Shape.IsFalling = false;
        gameOverPanel.SetActive(true);

        StartCoroutine(WaitingForNewGame());
    }

    private IEnumerator WaitingForNewGame()
    {
        yield return new WaitForSeconds(2f);
        yield return new WaitUntil(() => Input.anyKeyDown || Input.GetMouseButtonDown(0));
        NewGame();
    }

    private IEnumerator Pause()
    {
        Shape.IsFalling = false;
        gamePausedPanel.SetActive(true);
        yield return new WaitUntil(() => Input.anyKeyDown || Input.GetMouseButtonDown(0));
        Shape.IsFalling = true;
        gamePausedPanel.SetActive(false);
    }
    public void ButtonPause()
    {
        StartCoroutine(Pause());
    }

    public void ToggleSounds()
    {
        foreach (AudioSource source in _allAudioSources)
        {
            source.mute = !source.mute;
            soundButtonImage.sprite = source.mute ? spriteSoundOff : spriteSoundOn;
        }
    }
    
    public void UpdateBoard()
    {
        // If there's a block in the top-most row, Game Over
        for (var column = 0; column < BoardWidth; column++)
        {
            if (board[BoardHeight - 1, column] != 0)
            {
                GameOver();
                return;
            }
        }
        
        bool rowFull;
        _fullRows.Clear();
        foreach (var row in affectedRows)
        {
            rowFull = true;
            // Check if the row is full
            for (var column = 0; column < BoardWidth; column++)
            {
                if (board[row, column] != 0) continue;
                
                rowFull = false;
                break;
            }

            if (rowFull) _fullRows.Add(row);
        }
        _fullRows.Sort();

        // Score calculation
        switch (_fullRows.Count)
        {
            case 0:
                break;
            case 1:
                UpdateScore(100);
                break;
            case 2:
                UpdateScore(400);
                break;
            case 3:
                UpdateScore(1000);
                break;
            case 4:
                UpdateScore(3000);
                break;
        }
        
        
        // If there were any full lines, clear them and shift 
        // the lines above down
        if (_fullRows.Count > 0)
        {
            _audioSource.Play();
            var downwardShift = 0;
            for (var i = 0; i < _fullRows.Count; i++)
            {
                downwardShift++;
                var nextEmptyRow = (i == _fullRows.Count - 1) ? BoardHeight: _fullRows[i + 1];
                /*
                 *  Every row above a full row has to shift down the number of rows
                 *  equal to the number of full rows below it
                 * 
                 *  row 8    0, 0, 0, 0, 0, 0   -> not full row   
                 *  row 7    0, 0, 0, 0, 0, 0   -> not full row   
                 *  row 6    0, 0, 0, 0, 0, 0   -> not full row   
                 *  row 5    7, 0, 0, 0, 0, 0   -> not full row -> move 2 rows down  
                 *  row 4    7, 0, 0, 5, 5, 0   -> not full row -> move 2 rows down  
                 *  row 3    7, 4, 4, 4, 5, 5   -> full row   
                 *  row 2    7, 4, 3, 3, 0, 0   -> not full row -> move 1 row down   
                 *  row 1    7, 1, 3, 3, 2, 2   -> full row   
                 *  row 0    1, 1, 1, 2, 2, 0   -> not full row   
                 */
                for (var rowToMoveDown = _fullRows[i] + 1; rowToMoveDown < nextEmptyRow; rowToMoveDown++)
                {
                    for (var column = 0; column < BoardWidth; column++)
                    {
                        board[rowToMoveDown - 1 * downwardShift, column] = board[rowToMoveDown, column];
                    }
                }

                // Insert number of empty rows equal to the number of clear rows
                // at the top of the board
                for (var row = 0; row < _fullRows.Count; row++)
                {
                    for (var column = 0; column < BoardWidth; column++)
                    {
                        board[BoardHeight - (row + 1), column] = 0;
                    }
                }
            }
        }

        DrawBoard();
        affectedRows.Clear();
        SpawnShape();
    }

    private void UpdateScore(int scoreIncrease)
    {
        _score += scoreIncrease;
        textScore.text = _score.ToString();
    }

    private void DrawBoard()
    {
        // If no rows were cleared, we only need to update the rows
        // where the piece landed
        if (_fullRows.Count == 0)
        {
            foreach (var row in affectedRows)
            {
                for (var column = 0; column < BoardWidth; column++)
                {
                    _blocksOnBoard[row][column].color = shapeColors[board[row,column]];    
                }
            }
        }
        else {
            for (var row = 0; row < BoardHeight; row++)
            {
                for (var column = 0; column < BoardWidth; column++)
                {
                    _blocksOnBoard[row][column].color = shapeColors[board[row,column]];
                }
            }
        }
    }
}

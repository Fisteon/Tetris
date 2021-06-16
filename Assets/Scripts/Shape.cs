using System;
using UnityEngine;

public class Shape : MonoBehaviour
{
    private float _time;
    private float _touchDuration;
    private float _screenWidth;
    private float _screenHeight;
    private Vector2 _lastActionPosition;
    private const float ScreenPercentageRequiredToMove = 0.08f;
    private const float ScreenPercentageRequiredToDrop = 0.15f;
    private const float ScreenPercentageRequiredToFall = 0.1f;
    private const float TouchDurationRequiredToDrop    = 0.5f;
    private const float ScreenPercentageRequiredToRotate = 0.03f;
    public static bool IsFalling;

    private enum HorizontalDirection
    {
        Left = -1,
        Still = 0,
        Right = 1,
        
        DoubleLeft = -2,
        DoubleRight = 2,
    }

    private enum VerticalDirection
    {
        Up = 1,
        Down = -1,
        DoubleUp = 2
    }

    private enum RotationDirection
    {
        Clockwise = -90,
        CounterClockwise = 90
    }

    public AudioClip soundRotate;
    public AudioClip soundMove;

    private AudioSource _audioSource;

    private void Start()
    {
        _audioSource = this.GetComponent<AudioSource>();
        _screenHeight = Screen.height;
        _screenWidth = Screen.width;
    }
    private void Update()
    {
        _time += Time.deltaTime;
        
        if (_time >= GameManager.gameManager.fallTime && IsFalling)
        {
            _time = 0;
            MoveDown();
        }


        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                _lastActionPosition = Vector2.zero;
                _touchDuration = 0;
            }
            
            else if (touch.phase == TouchPhase.Moved)
            {
                _lastActionPosition += touch.deltaPosition;
                _touchDuration += touch.deltaTime;

                // X-axis operations
                if (Math.Abs(_lastActionPosition.x) > Math.Abs(_lastActionPosition.y))
                {
                    var spacesToMove = Mathf.FloorToInt(_lastActionPosition.x / (_screenWidth * ScreenPercentageRequiredToMove));
                    for (var counter = 0; counter < Math.Abs(spacesToMove); counter++)
                    {
                        if (spacesToMove < 0)
                        {
                            if (IsMoveValid(HorizontalDirection.Left))
                            {
                                MoveShape(HorizontalDirection.Left);
                                PlaySound(soundMove);
                            }
                        }
                        else
                        {
                            if (IsMoveValid(HorizontalDirection.Right))
                            {
                                MoveShape(HorizontalDirection.Right);
                                PlaySound(soundMove);
                            }
                        }
                    }
                    _lastActionPosition.x -= spacesToMove * (_screenWidth * ScreenPercentageRequiredToMove);
                }
                // Y-axis operations
                else
                {
                    GameManager.gameManager.fallTime = (-_lastActionPosition.y > _screenHeight * ScreenPercentageRequiredToFall) ? 0.1f : 1f;
                }
            }

            else if (touch.phase == TouchPhase.Ended)
            {
                // Rotation - if clicked on the left part of the screen, perform CounterClockwise rotation,
                // else, perform Clockwise rotation
                if ((-touch.rawPosition + touch.position).magnitude < (_screenWidth * ScreenPercentageRequiredToRotate))
                {
                    bool rotateClockwise = touch.rawPosition.x > _screenWidth / 2f;
                    RotateShape(rotateClockwise 
                        ? RotationDirection.Clockwise
                        : RotationDirection.CounterClockwise);

                    // If initial position after rotation is valid, return
                    // Otherwise, perform Wall Kicking or Floor Kicking
                    if (IsRotationPossible())
                    {
                        PlaySound(soundRotate);
                        return;
                    }
            
                    RotateShape(rotateClockwise
                        ? RotationDirection.CounterClockwise
                        : RotationDirection.Clockwise);
                }
                
                // Dropping the piece
                else if ((touch.rawPosition.y - touch.position.y) > _screenHeight * ScreenPercentageRequiredToDrop 
                         && _touchDuration < TouchDurationRequiredToDrop)
                {
                    while (!MoveDown())
                    {
                    }
                    PlaySound(soundMove);
                }
                GameManager.gameManager.fallTime = 1f;
            }
        }
        
        #region Keyboard input
        // Left/Right movement
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (IsMoveValid(HorizontalDirection.Left))
            {
                MoveShape(HorizontalDirection.Left);
                PlaySound(soundMove);
            }
        }

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (IsMoveValid(HorizontalDirection.Right)){
                MoveShape(HorizontalDirection.Right);
                PlaySound(soundMove);
            }
        }

        // Rotation
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            RotateShape(RotationDirection.Clockwise);

            // If initial position after rotation is valid, return
            // Otherwise, perform Wall Kicking or Floor Kicking
            if (IsRotationPossible())
            {
                PlaySound(soundRotate);
                return;
            }
            
            RotateShape(RotationDirection.CounterClockwise);
        }
        
        // Drop the piece
        if (Input.GetKeyDown(KeyCode.Space))
        {
            while (!MoveDown())
            {
                
            }
            PlaySound(soundMove);
        }
        #endregion
    }

    private bool IsMoveValid(HorizontalDirection horizontalDirection, VerticalDirection verticalDirection = 0)
    {
        foreach (Transform block in transform)
        {
            var position = block.position;
            var i = Mathf.RoundToInt(position.y + (1 * (int) verticalDirection));
            var j = Mathf.RoundToInt(position.x + (1 * (int) horizontalDirection));

            if (i < 0 || i >= GameManager.BoardHeight || j < 0 || j >= GameManager.BoardWidth || GameManager.gameManager.board[i,j] != 0)
            {
                return false;
            }
        }
        return true;
    }

    private bool IsRotationPossible()
    {
        // If position is valid right after rotation, no action needed
        if (IsMoveValid(HorizontalDirection.Still))
        {
            return true;
        }
            
        // Wall Kicking
        if (IsMoveValid(HorizontalDirection.Left))
        {
            MoveShape(HorizontalDirection.Left);
            return true;
        }
        if (IsMoveValid(HorizontalDirection.Right))
        {
            MoveShape(HorizontalDirection.Right);
            return true;
        }

        if (IsMoveValid(HorizontalDirection.DoubleLeft))
        {
            MoveShape(HorizontalDirection.DoubleLeft);
            return true;
        }
        if (IsMoveValid(HorizontalDirection.DoubleRight))
        {
            MoveShape(HorizontalDirection.DoubleRight);
            return true;
        }
            
        // Floor Kicking
        if (IsMoveValid(HorizontalDirection.Still, VerticalDirection.Up))
        {
            MoveShape(HorizontalDirection.Still, VerticalDirection.Up);
            return true;
        }
        if (IsMoveValid(HorizontalDirection.Still, VerticalDirection.DoubleUp))
        {
            MoveShape(HorizontalDirection.Still, VerticalDirection.DoubleUp);
            return true;
        }

        return false;
    }

    private void PlaySound(AudioClip sound)
    {
        _audioSource.clip = sound;
        _audioSource.Play();
    }

    private void RotateShape(RotationDirection direction)
    {
        transform.Rotate(new Vector3(0,0, (int)direction), Space.World);
    }

    private void MoveShape(HorizontalDirection horizontalDirection, VerticalDirection verticalDirection = 0)
    {
        transform.Translate(new Vector3((int)horizontalDirection, (int)verticalDirection, 0), Space.World);
    }

    private bool MoveDown()
    {
        foreach (Transform block in transform)
        {
            var position = block.position;
            // If the block can't fall any further, place it on the board
            if (position.y <= 0 ||
                GameManager.gameManager.board[Mathf.RoundToInt(position.y - 1), Mathf.RoundToInt(position.x)] != 0)
            {
                FinalizeShape();
                return true;
            }
        }
        MoveShape(HorizontalDirection.Still, VerticalDirection.Down);
        return false;
    }

    private void FinalizeShape()
    {
        foreach (Transform block in transform)
        {
            var position= block.position;
            var row= Mathf.RoundToInt(position.y);
            var column= Mathf.RoundToInt(position.x);

            GameManager.gameManager.board[row, column] = GameManager.gameManager.currentShape + 1;
            GameManager.gameManager.affectedRows.Add(row);

            _time = 0;
            _lastActionPosition = Vector2.zero;
            GameManager.gameManager.fallTime = 1f;
            this.gameObject.SetActive(false);
        }

        GameManager.gameManager.UpdateBoard();
    }
}

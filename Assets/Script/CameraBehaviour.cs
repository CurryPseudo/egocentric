using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CameraBehaviour : MonoBehaviour
{
    public AnimationCurve enterSceneCurve;
    public AnimationCurve exitSceneCurve;
    bool _exitScene = false;
    public bool exitScene
    {
        get => _exitScene;
        set
        {
            currentTime = 0.0f;
            _exitScene = value;
        }
    }
    bool isRetry = false;

    new Camera camera;
    Player player;
    float originSize;
    float currentTime = 0;
    public float maxAngleSpeed = 360f;
    // Start is called before the first frame update
    void Start()
    {
        camera = GetComponent<Camera>();
        player = GameObject.FindObjectOfType<Player>();
        originSize = camera.orthographicSize;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R) && !exitScene)
        {
            isRetry = true;
            exitScene = true;
        }
        {
            var localPlayerUp = transform.InverseTransformDirection(player.up);
            var angle = Vector2.SignedAngle(Vector2.up, localPlayerUp);
            angle = Mathf.Min(Mathf.Abs(angle), maxAngleSpeed * Time.deltaTime) * Mathf.Sign(angle);
            transform.Rotate(Vector3.forward, angle);
            transform.position = new Vector3(player.pos.x, player.pos.y, transform.position.z);
        }
        {
            if (exitScene)
            {
                if (currentTime > exitSceneCurve.keys[exitSceneCurve.keys.Length - 1].time)
                {
                    if (isRetry)
                    {
                        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                    }
                    else
                    {
                        if (SceneManager.GetActiveScene().buildIndex < SceneManager.sceneCountInBuildSettings - 1)
                        {
                            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
                        }
                    }
                }
                else
                {
                    camera.orthographicSize = originSize * exitSceneCurve.Evaluate(currentTime);
                }

            }
            else
            {
                camera.orthographicSize = originSize * enterSceneCurve.Evaluate(currentTime);
            }
        }
        currentTime += Time.deltaTime;
    }
}

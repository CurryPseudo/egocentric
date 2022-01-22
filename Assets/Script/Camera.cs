using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Camera : MonoBehaviour
{
    public Player player;
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        var angle = Vector2.SignedAngle(player.up, Vector2.up);
        transform.rotation = Quaternion.Euler(0, 0, -angle);
        transform.position = new Vector3(player.pos.x, player.pos.y, transform.position.z);
    }
}

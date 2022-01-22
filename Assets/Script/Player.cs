using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

abstract class State
{
    internal Player player;
    public abstract IEnumerator Main();
    public virtual void DrawGizmos() { }
}

class Idle : State
{
    internal RaycastHit2D hit;
    internal Vector2 localOffset;
    public override void DrawGizmos()
    {
        var dis = Mathf.Max(player.onGroundEpsilon, -Vector2.Dot(player.velocity, player.up));
        Gizmos.DrawLine(player.foot, player.foot - player.up * dis);
    }
    public virtual bool SwitchState()
    {
        if (hit && player.shouldSelfCenter)
        {
            var current = hit.collider.transform;
            for (; current.parent != null; current = current.parent) ;
            player.Switch(new SelfCenter(current));
            return true;
        }
        return false;
    }
    public virtual void UpdateTransform()
    {
        player.pos = player.pos + player.worldDir(localOffset);
        if (hit)
        {
            player.up = hit.normal;
            player.localVelocity = new Vector2(player.localVelocity.x, 0);
        }

    }
    public virtual PlayerColor CurrentColor()
    {
        return player.idleColor;
    }
    public override IEnumerator Main()
    {
        while (true)
        {
            {

                var currentColor = CurrentColor();
                foreach (var renderer in player.OutlineRenderers)
                {
                    renderer.color = currentColor.outline;
                }
                foreach (var renderer in player.Renderers)
                {
                    renderer.color = currentColor.color;
                }
            }
            hit = new RaycastHit2D();
            {
                localOffset = player.localVelocity * Time.deltaTime;
                var dis = Mathf.Max(player.onGroundEpsilon, -localOffset.y);
                hit = Physics2D.CircleCast(player.pos + player.worldDir(new Vector2(localOffset.x, 0.0f)), player.radius, -player.up, dis, player.groundLayer);
                if (hit)
                {
                    localOffset.y = -hit.distance;
                }
                if (SwitchState())
                {
                    yield break;
                }
                UpdateTransform();
            }
            player.localVelocity -= Vector2.up * player.gravity * Time.deltaTime;
            {
                var horizontal = Input.GetAxisRaw("Horizontal");
                player.localVelocity += Vector2.right * player.inputAcceration * Time.deltaTime * horizontal;
            }
            if (hit)
            {
                var right_velocity_dec = player.friction * Time.deltaTime;
                if (Mathf.Abs(player.localVelocity.x) < right_velocity_dec)
                {
                    player.localVelocity = new Vector2(0, player.localVelocity.y);
                }
                else
                {
                    player.localVelocity -= Vector2.right * Mathf.Sign(player.localVelocity.x) * right_velocity_dec;
                }
            }
            if (Mathf.Abs(player.velocity.magnitude) < player.velocityEpsilon)
            {
                player.velocity = Vector2.zero;
            }
            if (Mathf.Abs(player.velocity.magnitude) > player.maxVelocity)
            {
                player.velocity = player.velocity.normalized * player.maxVelocity;
            }

            yield return new WaitForFixedUpdate();
        }
    }
}

class SelfCenter : Idle
{
    Transform sticked;
    public SelfCenter(Transform sticked)
    {
        this.sticked = sticked;
    }
    public override bool SwitchState()
    {
        if (!player.shouldSelfCenter)
        {
            player.Switch(new Idle());
            return true;
        }
        return false;

    }
    public override PlayerColor CurrentColor()
    {
        return player.selfCenterColor;
    }
    public override void UpdateTransform()
    {
        var worldOffset = player.worldDir(localOffset);
        sticked.position = sticked.position - new Vector3(worldOffset.x, worldOffset.y, 0.0f);
        if (hit)
        {
            var signedAngle = Vector2.SignedAngle(player.up, hit.normal);
            sticked.RotateAround(player.pos, Vector3.forward, -signedAngle);
            player.localVelocity = new Vector2(player.localVelocity.x, 0);
        }
    }

}

[Serializable]
public struct PlayerColor
{
    public Color color;
    public Color outline;
    public PlayerColor(Color color, Color outline)
    {
        this.color = color;
        this.outline = outline;
    }
}

public class Player : MonoBehaviour
{
    public Vector2 up = Vector2.up;
    public Vector2 right => new Vector2(up.y, -up.x);
    public Vector2 pos
    {
        get { return GetComponent<Rigidbody2D>().position; }
        set { GetComponent<Rigidbody2D>().position = value; }
    }
    public Vector2 velocity = Vector2.zero;
    public Vector2 localVelocity
    {
        get { return new Vector2(Vector2.Dot(velocity, right), Vector2.Dot(velocity, up)); }
        set
        {
            velocity = worldDir(value);
        }
    }
    public float gravity;
    public float onGroundEpsilon = 0.1f;
    public Vector2 foot => pos - up * radius;
    public float radius => GetComponent<CircleCollider2D>().radius;
    public new Collider2D collider => GetComponent<CircleCollider2D>();
    public LayerMask groundLayer;
    public float inputAcceration = 1.0f;
    public float friction = 1.0f;
    public float velocityEpsilon = 0.1f;
    public float maxVelocity = 3f;
    public bool shouldSelfCenter = false;
    public List<SpriteRenderer> OutlineRenderers = new List<SpriteRenderer>();
    public List<SpriteRenderer> Renderers = new List<SpriteRenderer>();
    public PlayerColor selfCenterColor = new PlayerColor(Color.white, Color.black);
    public PlayerColor idleColor = new PlayerColor(Color.white, Color.black);

    State state;

    // Start is called before the first frame update
    void Start()
    {
        Switch(new Idle());
    }
    internal void Switch(State newState)
    {
        if (state != null)
        {
            StopCoroutine(state.Main());
        }
        state = newState;
        state.player = this;
        StartCoroutine(state.Main());
    }
    void OnDrawGizmos()
    {
        if (state != null)
            state.DrawGizmos();

    }
    public Vector2 worldDir(Vector2 local)
    {
        return local.x * right + local.y * up;
    }
    // Update is called once per frame
    void Update()
    {
    }
}

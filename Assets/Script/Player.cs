using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

abstract class State
{
    internal Player player;
    public abstract IEnumerator Main();
    public virtual void DrawGizmos() { }
}

class Idle : State
{
    internal RaycastHit2D hit;
    internal Vector2 normalNotSmoothed;
    internal Vector2 localOffset;
    event Action onDrawGizmos;
    public override void DrawGizmos()
    {
        onDrawGizmos?.Invoke();
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
    public virtual bool UpdateTranslation()
    {
        player.pos = player.pos + player.worldDir(localOffset);
        return false;
    }
    public virtual bool UpdateRotation()
    {
        if (ShouldStick())
        {
            player.up = hit.normal;
        }
        return false;
    }
    public bool ShouldStick()
    {
        return hit;
    }
    public virtual PlayerColor CurrentColor()
    {
        return player.idleColor;
    }
    public void UpdateCurrentHit(IEnumerable<RaycastHit2D> hits)
    {
        foreach (var currentHit in hits)
        {
            if (!hit)
            {
                hit = currentHit;
            }
            else
            {
                if (currentHit.distance < hit.distance)
                {
                    hit = currentHit;
                }
            }
            var target = currentHit.collider.GetComponent<Target>();
            if (target != null)
            {
                hit = new RaycastHit2D();
            }
            var cameraBehaviour = GameObject.FindObjectOfType<CameraBehaviour>();
            if (target != null && target.isAlive && !cameraBehaviour.exitScene)
            {
                target.isAlive = false;
                cameraBehaviour.exitScene = true;
            }
        }
    }
    public override IEnumerator Main()
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
        while (true)
        {
            onDrawGizmos = null;
            {
                if (player.buttonSwitchSelfCenter)
                {
                    if (Input.GetButton("Stick") && !player.lastStick)
                    {
                        if (!player.shouldSelfCenter)
                        {
                            if (player.maxSelfCenterCount > 0)
                            {
                                player.maxSelfCenterCount--;
                                player.shouldSelfCenter = true;
                            }
                        }
                        else
                        {
                            player.shouldSelfCenter = false;
                        }
                    }
                    player.lastStick = Input.GetButton("Stick");
                }
            }
            hit = new RaycastHit2D();
            {
                localOffset = player.localVelocity * Time.deltaTime;
                {
                    var dis = Mathf.Abs(localOffset.x);
                    var origin = player.pos;
                    var hits = Physics2D.CircleCastAll(origin, player.radius, player.right * Mathf.Sign(localOffset.x), dis, player.groundLayer);
                    UpdateCurrentHit(hits);
                    if (hit)
                    {
                        onDrawGizmos += () =>
                        {
                            Gizmos.color = Color.red;
                            var dist = origin + player.right * Mathf.Sign(localOffset.x) * dis;
                            Gizmos.DrawWireSphere(origin, player.radius);
                            Gizmos.DrawWireSphere(dist, player.radius);
                            Gizmos.DrawSphere(hit.point, 0.1f);
                            Gizmos.DrawLine(hit.point, hit.point + hit.normal);
                        };
                        localOffset.x = Mathf.Sign(localOffset.x) * Mathf.Min(hit.distance - player.liftGroundEpsilon, 0f);
                    }
                    else
                    {
                        onDrawGizmos += () =>
                        {
                            Gizmos.color = Color.magenta;
                            var dist = origin + player.right * Mathf.Sign(localOffset.x) * dis;
                            Gizmos.DrawWireSphere(origin, player.radius);
                            Gizmos.DrawWireSphere(dist, player.radius);
                        };

                    }
                }
                if (!hit)
                {
                    var dis = Mathf.Max(player.onGroundEpsilon, -localOffset.y);
                    var hits = Physics2D.CircleCastAll(player.pos + player.worldDir(new Vector2(localOffset.x, 0.0f)), player.radius, -player.up, dis, player.groundLayer);
                    UpdateCurrentHit(hits);
                    if (hit)
                    {
                        onDrawGizmos += () =>
                        {
                            Gizmos.color = Color.yellow;
                            var origin = player.pos + player.worldDir(new Vector2(localOffset.x, 0.0f));
                            var dist = origin + -player.up * dis;
                            Gizmos.DrawWireSphere(origin, player.radius);
                            Gizmos.DrawWireSphere(dist, player.radius);
                            Gizmos.DrawSphere(hit.point, 0.1f);
                            Gizmos.DrawLine(hit.point, hit.point + hit.normal);
                        };
                        localOffset.y = Mathf.Min(-hit.distance + player.liftGroundEpsilon, 0f);
                    }
                }
                if (hit)
                {
                    var box = hit.collider as BoxCollider2D;
                    if (box)
                    {
                        Vector2 boxLocalPoint = box.transform.InverseTransformPoint(hit.point);
                        var dir = boxLocalPoint - box.offset;
                        if (Mathf.Abs(dir.y / dir.x) >= box.size.y / box.size.x)
                        {
                            if (dir.y >= 0)
                            {
                                normalNotSmoothed = box.transform.TransformDirection(Vector2.up);
                            }
                            else
                            {
                                normalNotSmoothed = box.transform.TransformDirection(Vector2.down);
                            }
                        }
                        else
                        {
                            if (dir.x >= 0)
                            {
                                normalNotSmoothed = box.transform.TransformDirection(Vector2.right);
                            }
                            else
                            {
                                normalNotSmoothed = box.transform.TransformDirection(Vector2.left);
                            }

                        }
                    }
                    else
                    {
                        normalNotSmoothed = hit.normal;
                    }
                }
                if (SwitchState())
                {
                    yield break;
                }
                var angleOffset = -localOffset.x / (2 * Mathf.PI * player.radius) * 360;
                player.transform.rotation = player.transform.rotation * Quaternion.AngleAxis(angleOffset, Vector3.forward);
                if (UpdateTranslation()) { yield break; }
                if (UpdateRotation()) { yield break; }
                if (hit)
                {
                    player.localVelocity = new Vector2(player.localVelocity.x, 0);
                }
            }
            player.localVelocity -= Vector2.up * player.gravity * Time.deltaTime;
            if (ShouldStick())
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
    public override bool UpdateTranslation()
    {
        var worldOffset = player.worldDir(localOffset);
        RaycastHit2D hit = new RaycastHit2D();
        var currentColliders = sticked.GetComponentsInChildren<Collider2D>();
        foreach (var collider in currentColliders)
        {
            RaycastHit2D[] hits = new RaycastHit2D[10];
            var num = collider.Cast(worldOffset.normalized, hits, worldOffset.magnitude, false);
            for (int i = 0; i < num; i++)
            {
                var currentHit = hits[i];
                if ((player.groundLayer.value & (1 << currentHit.transform.gameObject.layer)) == 0) continue;
                var selfCollider = false;
                for (int j = 0; j < currentColliders.Length; j++)
                {
                    if (currentColliders[j] == currentHit.collider)
                    {
                        selfCollider = true;
                        break;
                    }
                }
                if (selfCollider) continue;
                if (!hit)
                {
                    hit = currentHit;
                }
                if (currentHit.distance < hit.distance)
                {
                    hit = currentHit;
                }
            }
        }
        if (hit)
        {
            worldOffset = worldOffset.normalized * hit.distance;
            sticked.position = sticked.position - new Vector3(worldOffset.x, worldOffset.y, 0.0f);
            player.shouldSelfCenter = false;
            player.Switch(new Idle());
            return true;
        }
        sticked.position = sticked.position - new Vector3(worldOffset.x, worldOffset.y, 0.0f);
        return false;
    }
    public override bool UpdateRotation()
    {
        if (hit)
        {
            var signedAngle = Vector2.SignedAngle(player.up, hit.normal);
            sticked.RotateAround(player.pos, Vector3.forward, -signedAngle);
        }
        return false;
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
    public bool InnerEnable = true;
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
    public float liftGroundEpsilon = 0.1f;
    public Vector2 foot => pos - up * radius;
    public float radius => GetComponent<CircleCollider2D>().radius;
    public new Collider2D collider => GetComponent<CircleCollider2D>();
    public LayerMask groundLayer;
    public float inputAcceration = 1.0f;
    public float friction = 1.0f;
    public float velocityEpsilon = 0.1f;
    public float maxVelocity = 3f;
    public bool shouldSelfCenter = false;
    public bool buttonSwitchSelfCenter = false;
    public List<SpriteRenderer> OutlineRenderers = new List<SpriteRenderer>();
    public List<SpriteRenderer> Renderers = new List<SpriteRenderer>();
    public PlayerColor selfCenterColor = new PlayerColor(Color.white, Color.black);
    public PlayerColor idleColor = new PlayerColor(Color.white, Color.black);
    public float maxUpAngleDiff = 30.0f;
    public int maxSelfCenterCount = 0;
    internal bool lastStick = false;

    State state;

    // Start is called before the first frame update
    void Start()
    {
        if (InnerEnable)
        {
            Switch(new Idle());
        }
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
